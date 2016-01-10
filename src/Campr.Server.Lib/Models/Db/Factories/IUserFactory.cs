namespace Campr.Server.Lib.Models.Db.Factories
{
    public interface IUserFactory
    {
        User CreateUserFromEntity(string entity);
        User CreateUserFromHandle(string handle);
    }
}