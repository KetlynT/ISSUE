namespace GraficaModerna.Application.Validators;

public static class DocumentValidator
{
    public static bool IsValid(string doc)
    {
        if (string.IsNullOrWhiteSpace(doc)) return false;

        var cleanDoc = doc.Replace(".", "").Replace("-", "").Replace("/", "").Trim();

        if (cleanDoc.Length == 11)
            return IsCpfValid(cleanDoc);
        
        if (cleanDoc.Length == 14)
            return IsCnpjValid(cleanDoc);

        return false;
    }

    private static bool IsCpfValid(string cpf)
    {
        if (cpf.Distinct().Count() == 1) return false;

        var multiplier1 = new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplier2 = new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var tempCpf = cpf[..9];
        var sum = 0;

        for (var i = 0; i < 9; i++)
            sum += int.Parse(tempCpf[i].ToString()) * multiplier1[i];

        var remainder = sum % 11;
        if (remainder < 2)
            remainder = 0;
        else
            remainder = 11 - remainder;

        var digit = remainder.ToString();
        tempCpf += digit;
        sum = 0;

        for (var i = 0; i < 10; i++)
            sum += int.Parse(tempCpf[i].ToString()) * multiplier2[i];

        remainder = sum % 11;
        if (remainder < 2)
            remainder = 0;
        else
            remainder = 11 - remainder;

        digit += remainder.ToString();

        return cpf.EndsWith(digit);
    }

    private static bool IsCnpjValid(string cnpj)
    {
        var multiplier1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplier2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var tempCnpj = cnpj[..12];
        var sum = 0;

        for (var i = 0; i < 12; i++)
            sum += int.Parse(tempCnpj[i].ToString()) * multiplier1[i];

        var remainder = sum % 11;
        if (remainder < 2)
            remainder = 0;
        else
            remainder = 11 - remainder;

        var digit = remainder.ToString();
        tempCnpj += digit;
        sum = 0;

        for (var i = 0; i < 13; i++)
            sum += int.Parse(tempCnpj[i].ToString()) * multiplier2[i];

        remainder = sum % 11;
        if (remainder < 2)
            remainder = 0;
        else
            remainder = 11 - remainder;

        digit += remainder.ToString();

        return cnpj.EndsWith(digit);
    }
}