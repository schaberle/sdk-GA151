using System;
using System.IO;
using LeituraDigital;

static class Capturar
{
    static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--verificar")
        {
            bool ok = Leitor.ImagemPronta(7, 0)
                && Leitor.ImagemPronta(4, 1)
                && !Leitor.ImagemPronta(4, 0)
                && !Leitor.ImagemPronta(2, 1);
            Console.WriteLine(ok ? "verificacao ok" : "verificacao falhou");
            return ok ? 0 : 1;
        }

        string destino = args.Length > 0 ? args[0] : "captura.raw";
        Console.WriteLine("Apoie o dedo no sensor.");
        using (Leitor leitor = new Leitor())
        {
            leitor.Abrir();
            File.WriteAllBytes(destino, leitor.Capturar(TimeSpan.FromSeconds(60)));
        }
        Console.WriteLine("Imagem salva em " + Path.GetFullPath(destino));
        return 0;
    }
}
