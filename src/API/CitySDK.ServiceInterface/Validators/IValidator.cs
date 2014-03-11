namespace CitySDK.ServiceInterface.Validators
{
    public interface IValidator<in T>
    {
        IMongoDB MongoDB { get; set; }

        bool Validate(string verb, T obj, out string msg);
    }
}
