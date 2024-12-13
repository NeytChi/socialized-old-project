using Domain.InstagramAccounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UseCases.InstagramAccounts
{
    public interface IIGAccountRepository
    {
        IGAccount GetBy(string userToken, string instagramUsername);

    }
}
