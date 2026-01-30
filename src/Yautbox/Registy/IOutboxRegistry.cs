namespace Yautbox.Registy;

internal interface IOutboxRegistry
{
    string GetIdentifier<T>();
}
