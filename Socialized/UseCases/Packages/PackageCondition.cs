using System;
using Serilog;
using Braintree;
using System.Linq;
using Serilog.Core;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using socialized;
using database.context;
using Models.Common;
using Models.SessionComponents;
using Domain.SessionComponents;

namespace UseCases.Packages
{
    public class PackageCondition
    {
        private ILogger Logger;
        public static BraintreeGateway gateway;
        
        public PackageCondition(ILogger logger, BrainTreeSettings treeSettings)
        {
            Logger = logger;
            gateway = new BraintreeGateway
            {
                Environment = treeSettings.BraintreeEnvironment == 0 ?
                    Braintree.Environment.SANDBOX : Braintree.Environment.PRODUCTION,
                MerchantId = treeSettings.MerchantId,
                PublicKey = treeSettings.PublicKey,
                PrivateKey = treeSettings.PrivateKey
            };
        }
        public ServiceAccess CreateFreeAccess(int userId)
        {
            var package = packages[0];
            var access = new ServiceAccess()
            {
                userId = userId,
                available = true,
                packageType = package.package_id,
                paid = false
            };
            context.ServiceAccess.Add(access);
            context.SaveChanges();
            Logger.Information("Create a free package, id -> " + access.accessId);
            return access;
        }
        public void SetPackage(int userId, int packageId, int monthCount)
        {
            var access = context.ServiceAccess.Where(sa => sa.userId == userId).FirstOrDefault()

            if (access == null)
            {
                access = CreateFreeAccess(userId);
            }
            var package = GetPackageById(packageId);

            access = UpdateAccess(access, package, monthCount);
        }
        public ServiceAccess UpdateAccess(ServiceAccess access, PackageAccess package, int monthCount)
        {
            access.available = true;
            access.packageType = package.package_id;
            access.paid = true;
            access.paidAt = DateTime.Now;
            access.disableAt = DateTime.Now.AddMonths(monthCount);
            context.ServiceAccess.Update(access);
            context.SaveChanges();
            Logger.Information("Update package from " + access.packageType
                 + " to " + package.package_id + ", id -> " + access.accessId);
            return access;
        }
        public bool IGAccountsIsTrue(User user, ref string message)
        {
            ServiceAccess access;
            PackageAccess package;

            if ((access = GetServiceAccess(user.userId)) != null)
            {
                package = GetPackageById(access.packageType);
                if (package.package_ig_accounts == -1)
                {
                    return true;
                }
                var accounts = from account in context.IGAccounts
                join u in context.Users on account.userId equals user.userId
                where u.userId == user.userId
                    && account.accountDeleted == false
                select account).ToList();
                if (accounts.Count < package.package_ig_accounts)
                {
                    return true;
                }
                else
                {
                    message = "Instagram accounts can't be more that " + package.package_ig_accounts + ".";
                }
            }
            return false;
        }
        public bool PostsIsTrue(int userId, ref string message)
        {
            ServiceAccess access;
            PackageAccess package;

            if ((access = GetServiceAccess(userId)) != null)
            {
                package = GetPackageById(access.packageType);
                if (package.package_posts == -1)
                {
                    return true;
                }
                var posts = (from post in context.AutoPosts
                 join account in context.IGAccounts on post.sessionId equals account.accountId
                 join user in context.Users on account.userId equals user.userId
                 where user.userId == userId
                     && post.postType
                 select post).ToList();
                if (posts.Count < package.package_posts)
                {
                    return true;
                }
                else
                {
                    message = "User doesn't have access to auto-posting service. Posts can't be more that " + package.package_posts + ".";
                }
            }
            return false;
        }
        public bool StoriesIsTrue(int userId, ref string message)
        {
            ServiceAccess access;
            PackageAccess package;

            if ((access = GetServiceAccess(userId)) != null)
            {
                package = GetPackageById(access.packageType);
                if (package.package_stories == -1)
                {
                    return true;
                }
                var posts = (from post in context.AutoPosts
                 join account in context.IGAccounts on post.sessionId equals account.accountId
                 join user in context.Users on account.userId equals user.userId
                 where user.userId == userId
                     && !post.postType
                 select post).ToList();
                if (posts.Count < package.package_stories)
                {
                    return true;
                }
                else
                {
                    message = "User doesn't have access to auto-posting service. Stories can't be more that " + package.package_stories + ".";
                }
            }
            return false;
        }
        /// <summary>
        /// <return>Count of days for receiving statistics. 
        /// (-1) - all days, 1..Max - receiving days, 0 - no one days</return>
        /// </summary>
        public int AnalyticsDays(int userId, ref string message)
        {
            ServiceAccess access;
            PackageAccess package;

            if ((access = GetServiceAccess(userId)) != null)
            {
                package = GetPackageById(access.packageType);
                return package.analytics_days;
            }
            return 0;
        }
        public ServiceAccess GetServiceAccess(int userId)
        {
            var access = context.ServiceAccess.Where(sa => sa.userId == userId).First();
            if (access.packageType != 1 && access.disableAt < DateTime.Now)
            {
                SetPackage(userId, 1, -1);
                Logger.Information("Package has expired, id -> " + access.accessId);
            }
            return access;
        }
        public bool PayForPackage(decimal price, string nonceToken, string deviceData, ref string message)
        {
            var request = new TransactionRequest
            {
                Amount = price,
                PaymentMethodNonce = nonceToken,
                DeviceData = deviceData,
                Options = new TransactionOptionsRequest
                {
                    SubmitForSettlement = true
                }
            };
            var result = gateway.Transaction.Sale(request);
            if (result.IsSuccess())
            {
                Logger.Information("Pay for package");
                return true;
            }
            else
            {
                message = result.Message;
            }
            return false;
        }
        public decimal CalcPackagePrice(PackageAccess package, int monthCount)
        {
            decimal discountPrice = 0; DiscountPackage discount;

            if ((discount = GetDiscountByMonth(monthCount)).discount_id != 0)
            {
                discountPrice = (decimal)(package.package_price * discount.discount_month / 100 * discount.discount_percent);
            }

            Logger.Information("Calc package price, id -> " + package.package_id);
            return (decimal)package.package_price * monthCount - discountPrice;
        }
        public PackageAccess GetPackageById(int packageId)
        {
            return packages.Where(p => p.package_id == packageId).FirstOrDefault();
        }
        public DiscountPackage GetDiscountByMonth(int monthCount)
        {
            return discounts.Where(d => monthCount >= d.discount_month).OrderByDescending(d => d.discount_month).FirstOrDefault();
        }
    }
}








