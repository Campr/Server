using System;

namespace Campr.Server.Lib.Models.Db.Factories
{
    public interface IBewitFactory
    {
        Bewit FromExpirationDate(DateTime expiresAt);
    }
}