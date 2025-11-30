namespace GraficaModerna.Domain.Entities;

public class SiteSetting
{
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    protected SiteSetting() { }

    public SiteSetting(string key, string value)
    {
        Key = key;
        Value = value;
    }

    // Método para atualizar o valor
    public void UpdateValue(string value)
    {
        Value = value;
    }
}