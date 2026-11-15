namespace Libreria.Servicio.Ventas.Application.Services;

public static class NumeroALetras
{
    public static string Convertir(decimal monto)
    {
        if (monto < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(monto),
                "El monto no puede ser negativo.");
        }

        if (monto > 999_999_999.99m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(monto),
                "El monto es demasiado grande para convertirlo a letras.");
        }

        monto = decimal.Round(monto, 2, MidpointRounding.AwayFromZero);

        var parteEntera = (long)decimal.Truncate(monto);
        var centavos = (int)((monto - parteEntera) * 100);

        var letras = ConvertirEntero(parteEntera);

        return $"Son: {letras} con {centavos:00}/100 bolivianos";
    }

    private static string ConvertirEntero(long numero)
    {
        if (numero == 0)
        {
            return "cero";
        }

        if (numero < 1000)
        {
            return ConvertirMenorMil((int)numero);
        }

        if (numero < 1_000_000)
        {
            var miles = numero / 1000;
            var resto = numero % 1000;

            var textoMiles = miles == 1
                ? "mil"
                : $"{ConvertirMenorMil((int)miles)} mil";

            return resto == 0
                ? textoMiles
                : $"{textoMiles} {ConvertirMenorMil((int)resto)}";
        }

        var millones = numero / 1_000_000;
        var restoMillones = numero % 1_000_000;

        var textoMillones = millones == 1
            ? "un millón"
            : $"{ConvertirEntero(millones)} millones";

        return restoMillones == 0
            ? textoMillones
            : $"{textoMillones} {ConvertirEntero(restoMillones)}";
    }

    private static string ConvertirMenorMil(int numero)
    {
        if (numero == 0)
        {
            return string.Empty;
        }

        if (numero == 100)
        {
            return "cien";
        }

        if (numero > 100)
        {
            var centenas = numero / 100;
            var resto = numero % 100;

            var textoCentena = centenas switch
            {
                1 => "ciento",
                2 => "doscientos",
                3 => "trescientos",
                4 => "cuatrocientos",
                5 => "quinientos",
                6 => "seiscientos",
                7 => "setecientos",
                8 => "ochocientos",
                9 => "novecientos",
                _ => string.Empty
            };

            return resto == 0
                ? textoCentena
                : $"{textoCentena} {ConvertirMenorCien(resto)}";
        }

        return ConvertirMenorCien(numero);
    }

    private static string ConvertirMenorCien(int numero)
    {
        if (numero < 30)
        {
            return numero switch
            {
                0 => string.Empty,
                1 => "uno",
                2 => "dos",
                3 => "tres",
                4 => "cuatro",
                5 => "cinco",
                6 => "seis",
                7 => "siete",
                8 => "ocho",
                9 => "nueve",
                10 => "diez",
                11 => "once",
                12 => "doce",
                13 => "trece",
                14 => "catorce",
                15 => "quince",
                16 => "dieciséis",
                17 => "diecisiete",
                18 => "dieciocho",
                19 => "diecinueve",
                20 => "veinte",
                21 => "veintiuno",
                22 => "veintidós",
                23 => "veintitrés",
                24 => "veinticuatro",
                25 => "veinticinco",
                26 => "veintiséis",
                27 => "veintisiete",
                28 => "veintiocho",
                29 => "veintinueve",
                _ => string.Empty
            };
        }

        var decena = numero / 10;
        var unidad = numero % 10;

        var textoDecena = decena switch
        {
            3 => "treinta",
            4 => "cuarenta",
            5 => "cincuenta",
            6 => "sesenta",
            7 => "setenta",
            8 => "ochenta",
            9 => "noventa",
            _ => string.Empty
        };

        return unidad == 0
            ? textoDecena
            : $"{textoDecena} y {ConvertirMenorCien(unidad)}";
    }
}