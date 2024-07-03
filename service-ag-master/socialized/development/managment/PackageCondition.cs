using System;
using Braintree;
using System.Linq;
using Serilog.Core;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using socialized;
using database.context;
using Models.Common;
using Models.SessionComponents;

namespace Managment
{
    public class PackageCondition
    {
        public static BraintreeGateway gateway;
        private Context context;
        private Logger log;
        private IConfiguration configuration;
        private List<PackageAccess> packages;
        private List<DiscountPackage> discounts;
        
        public PackageCondition(Context context, Logger log)
        {
            this.log = log;
            this.context = context;
            configuration = Program.serverConfiguration();
            gateway = new BraintreeGateway {
                Environment = configuration.GetValue<sbyte>("braintree_environment") == 0 ? 
                    Braintree.Environment.SANDBOX : Braintree.Environment.PRODUCTION,
                MerchantId = configuration.GetValue<string>("merchant_id"),
                PublicKey = configuration.GetValue<string>("public_key"),
                PrivateKey = configuration.GetValue<string>("private_key")
            };           
            packages = configuration.GetSection("service_access:packages")
                .GetChildren()
                .Select(x => new PackageAccess() {
                    package_id = x.GetValue<int>("package_id"),
                    package_name = x.GetValue<string>("package_name"),
                    package_price = x.GetValue<double>("package_price"),
                    package_ig_accounts = x.GetValue<int>("package_ig_accounts"),
                    package_posts = x.GetValue<int>("package_posts"),
                    package_stories = x.GetValue<int>("package_stories"),
                    analytics_days = x.GetValue<int>("analytics_days")
                }).ToList();
            discounts = configuration.GetSection("service_access:discounts")
                .GetChildren()
                .Select(x => new DiscountPackage() {
                    discount_id = x.GetValue<int>("discount_id"),
                    discount_percent = x.GetValue<double>("discount_percent"),
                    discount_day = x.GetValue<int>("discount_day"),
                    discount_month = x.GetValue<int>("discount_month")
                }).ToList();
        }
        public List<PackageAccess> GetPackages()
        {
            return packages;
        }
        public List<DiscountPackage> GetDiscounts()
        {
            return discounts;
        }
        public ServiceAccess CreateFreeAccess(int userId)
        {
            PackageAccess package = packages[0];
            ServiceAccess access = new ServiceAccess() {
                userId = userId,
                available = true,
                packageType = package.package_id,
                paid = false
            };
            context.ServiceAccess.Add(access);
            context.SaveChanges();
            log.Information("Create a free package, id -> " + access.accessId);
            return access;
        }
        public void SetPackage(int userId, int packageId, int monthCount)
        {
            ServiceAccess access;

            if ((access = context.ServiceAccess.Where(sa 
                    => sa.userId == userId).FirstOrDefault()) == null)
                access = CreateFreeAccess(userId);
            PackageAccess package = GetPackageById(packageId);

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
            log.Information("Update package from " + access.packageType
                 + " to " + package.package_id + ", id -> " + access.accessId);
            return access;
        }
        public bool IGAccountsIsTrue(User user, ref string message)
        {
            ServiceAccess access; 
            PackageAccess package;

            if ((access = GetServiceAccess(user.userId)) != null) {
                package = GetPackageById(access.packageType);
                if (package.package_ig_accounts == -1) 
                    return true;

                else if ((from account in context.IGAccounts
                        join u in context.Users on account.userId equals user.userId
                    where u.userId == user.userId
                        && account.accountDeleted == false
                        select account).ToList().Count < package.package_ig_accounts)
                    return true;
                else
                    message = "Instagram accounts can't be more that " + package.package_ig_accounts + ".";
            }
            return false;
        }
        public bool PostsIsTrue(int userId, ref string message)
        {
            ServiceAccess access;
            PackageAccess package;

            if ((access = GetServiceAccess(userId)) != null) {
                package = GetPackageById(access.packageType);
                if (package.package_posts == -1)
                    return true;

                else if ((from post in context.AutoPosts
                        join account in context.IGAccounts on post.sessionId equals account.accountId
                        join user in context.Users on account.userId equals user.userId
                    where user.userId == userId
                        && post.postType
                        select post).ToList().Count < package.package_posts)
                    return true;
                else
                    message = "User doesn't have access to auto-posting service. Posts can't be more that " + package.package_posts + ".";
            }
            return false;
        }
        public bool StoriesIsTrue(int userId, ref string message)
        {
            ServiceAccess access;
            PackageAccess package;

            if ((access = GetServiceAccess(userId)) != null) {
                package = GetPackageById(access.packageType);
                if (package.package_stories == -1)
                    return true;

                else if ((from post in context.AutoPosts
                        join account in context.IGAccounts on post.sessionId equals account.accountId
                        join user in context.Users on account.userId equals user.userId
                    where user.userId == userId
                        && !post.postType
                        select post).ToList().Count < package.package_stories)
                    return true;
                else
                    message = "User doesn't have access to auto-posting service. Stories can't be more that " + package.package_stories + ".";
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

            if ((access = GetServiceAccess(userId)) != null) {
                package = GetPackageById(access.packageType);
                return package.analytics_days;
            }
            return 0;
        }
        public ServiceAccess GetServiceAccess(int userId)
        {
            ServiceAccess access = context.ServiceAccess.Where(sa 
                => sa.userId == userId).First();
            if (access.packageType != 1  && access.disableAt < DateTime.Now) {
                SetPackage(userId, 1, -1);
                log.Information("Package has expired, id -> " + access.accessId);
            }
            return access;
        }
        public bool PayForPackage(decimal price, string nonceToken, string deviceData, ref string message)
        {
            TransactionRequest request = new TransactionRequest {
                Amount = price,
                PaymentMethodNonce = nonceToken,
                DeviceData = deviceData,
                Options = new TransactionOptionsRequest {
                    SubmitForSettlement = true
                }
            };
            Result<Transaction> result = gateway.Transaction.Sale(request);
            if (result.IsSuccess()) {
                log.Information("Pay for package");
                return true;
            }
            else
                message = result.Message; 
            return false;
        }
        public decimal CalcPackagePrice(PackageAccess package, int monthCount)
        {
            decimal discountPrice = 0; DiscountPackage discount;

            if ((discount = GetDiscountByMonth(monthCount)).discount_id != 0)
                discountPrice = (decimal)(package.package_price * discount.discount_month / 100 * discount.discount_percent);
            
            log.Information("Calc package price, id -> " + package.package_id);
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








