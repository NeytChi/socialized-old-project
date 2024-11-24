using System;
using System.Linq;
using System.Web;
using System.Collections.Generic;
using Serilog;
using Serilog.Core;
using Microsoft.AspNetCore.Mvc;

using database.context;
using Models.AutoPosting;
using Models.SessionComponents;

namespace WebAPI.Controllers
{
    [Route("v1.0/autopost/[controller]/[action]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly Context context;
        public Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        public CategoryController(Context context)
        {
            this.context = context;
        }
        [HttpPost]
        [ActionName("Create")]
        public ActionResult<dynamic> Create(CategoryCache cache)
        {
            string message = null; IGAccount account;
            Category category;

            if ((account = GetNonDeleteAccount(cache.user_token, cache.account_id, ref message)) != null)
            {
                if (!string.IsNullOrEmpty(cache.category_name)
                    && !string.IsNullOrEmpty(cache.category_color))
                {
                    cache.category_name = HttpUtility.UrlDecode(cache.category_name);
                    cache.category_color = HttpUtility.UrlDecode(cache.category_color);
                    if ((category = context.Categories.Where(c => c.accountId == account.accountId
                        && !c.categoryDeleted
                        && c.categoryName == cache.category_name)
                        .FirstOrDefault()) == null)
                    {
                        if (cache.category_name.Length <= 20
                            && cache.category_color.Length <= 20)
                        {
                            category = new Category()
                            {
                                accountId = account.accountId,
                                categoryName = cache.category_name,
                                categoryColor = cache.category_color,
                                createdAt = DateTimeOffset.UtcNow
                            };
                            context.Categories.Add(category);
                            context.SaveChanges();
                            log.Information("Create new category for autopost, id -> " + category.categoryId);
                            return new
                            {
                                success = true,
                                data = new
                                {
                                    category = new
                                    {
                                        category_id = category.categoryId,
                                        category_name = category.categoryName,
                                        category_color = category.categoryColor
                                    }
                                }
                            };
                        }
                        else
                            message = "Category name can't be more 20 characters";
                    }
                    else
                        message = "You can't create category with same name.";
                }
                else
                    message = "Category name can't be empty.";
            }
            return Return500Error(message);
        }
        [HttpGet]
        [ActionName("")]
        public ActionResult<dynamic> GetCategories([FromQuery] long account_id, [FromQuery] int since = 0, [FromQuery] int count = 50)
        {
            string message = null, userToken;
            IGAccount account;

            userToken = HttpContext?.Request.Headers.Where(h
                => h.Key == "Authorization").Select(h => h.Value)
                .FirstOrDefault()
                ?? "Bearer " + Testing.TestMockingContext.values.user_token;

            if (!string.IsNullOrEmpty(userToken) && userToken.Contains("Bearer "))
                userToken = userToken.Remove(0, 7);

            if ((account = GetNonDeleteAccount(userToken, account_id, ref message)) != null)
            {
                return new
                {
                    success = true,
                    data = new
                    {
                        categories = context.Categories.Where(c => c.accountId == account.accountId
                            && !c.categoryDeleted)
                        .Select(c => new
                        {
                            category_id = c.categoryId,
                            category_name = c.categoryName,
                            category_color = c.categoryColor
                        })
                        .Skip(count * since)
                        .Take(count)
                        .ToList()
                    }
                };
            }
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("Remove")]
        public ActionResult<dynamic> Remove(CategoryCache cache)
        {
            string message = null;
            IGAccount account;
            Category category;

            if ((account = GetNonDeleteAccount(cache.user_token, cache.account_id, ref message)) != null)
            {
                if ((category = context.Categories.Where(c => c.categoryId == cache.category_id
                    && !c.categoryDeleted).FirstOrDefault()) != null)
                {
                    category.categoryDeleted = true;
                    context.Categories.Update(category);
                    context.SaveChanges();
                    log.Information("Remove category, id -> " + category.categoryId);
                    return new { success = true };
                }
                else
                    message = "Server can't define category by id.";
            }
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("RemoveAll")]
        public ActionResult<dynamic> RemoveAll(CategoryCache cache)
        {
            string message = null;
            IGAccount account;
            List<Category> categories;

            if ((account = GetNonDeleteAccount(cache.user_token, cache.account_id, ref message)) != null)
            {
                categories = context.Categories.Where(c => c.accountId == account.accountId
                    && !c.categoryDeleted).ToList();
                foreach (Category category in categories)
                    category.categoryDeleted = true;
                context.Categories.UpdateRange(categories);
                context.SaveChanges();
                log.Information("Remove all categories by account id -> " + account.accountId);
                return new { success = true };
            }
            return Return500Error(message);
        }
        public IGAccount GetNonDeleteAccount(string userToken, long accountId, ref string message)
        {
            IGAccount account;

            account = (from a in context.IGAccounts
                       join u in context.Users on a.userId equals u.userId
                       where a.accountId == accountId
                           && !a.accountDeleted
                           && u.userToken == userToken
                       select a)
                .FirstOrDefault();
            if (account == null)
                message = "Server can't define account by id.";
            return account;
        }
        public dynamic Return500Error(string message)
        {
            if (Response != null)
                Response.StatusCode = 500;
            log.Warning(message + " IP -> " +
                HttpContext?.Connection.RemoteIpAddress.ToString() ?? "");
            return new { success = false, message };
        }
    }
}