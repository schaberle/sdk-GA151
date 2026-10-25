using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace LeituraDigital
{
public sealed class Leitor : IDisposable
{
    public const int Largura = 256;
    public const int Altura = 288;
    public const int Tamanho = Largura * Altura;

    bool aberto;

    public void Abrir()
    {
        if (aberto) return;
        if (Motor.OpenDevice(0, 0, 0) != 1)
            throw new IOException("Leitor indisponível. Confira o USB e feche outro programa que esteja usando o sensor.");
        if (Motor.LinkDevice(0) != 1)
        {
            Motor.CloseDevice();
            throw new IOException("O leitor não respondeu.");
        }
        int largura = 0, altura = 0;
        Motor.GetImageSize(ref largura, ref altura);
        if (largura != Largura || altura != Altura)
        {
            Motor.CloseDevice();
            throw new IOException("Tamanho inesperado: " + largura + "x" + altura + ".");
        }
        aberto = true;
    }

    public byte[] Capturar(TimeSpan espera)
    {
        if (!aberto) throw new InvalidOperationException("Abra o leitor antes de capturar.");
        if (espera <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("espera");
        // shortcut: o sensor fica ligado; timeout 0 impede a DLL de encerrar a espera sozinha.
        Motor.SetTimeOut(0);
        int ultima = -1, ultimoRetorno = -1;
        DateTime limite = DateTime.UtcNow + espera;
        Motor.CaptureImage();
        while (DateTime.UtcNow < limite)
        {
            int mensagem = Motor.GetWorkMsg();
            int retorno = Motor.GetRetMsg();
            if (mensagem == ultima && retorno == ultimoRetorno)
            {
                Thread.Sleep(40);
                continue;
            }
            ultima = mensagem;
            ultimoRetorno = retorno;
            if (mensagem == 1)
                throw new IOException("O leitor foi desconectado.");
            if (!ImagemPronta(mensagem, retorno)) continue;
            int largura = 0, altura = 0;
            Motor.GetImageSize(ref largura, ref altura);
            byte[] bruto = new byte[Math.Max(Tamanho, largura * altura)];
            int tamanho = bruto.Length;
            if (Motor.GetRawImage(bruto, ref tamanho) != 1 || largura != Largura || altura != Altura || tamanho != Tamanho)
                throw new IOException("A imagem não veio em 256x288.");
            if (bruto.Length != Tamanho) Array.Resize(ref bruto, Tamanho);
            return bruto;
        }
        throw new TimeoutException("Nenhum dedo chegou a tempo.");
    }

    public static bool ImagemPronta(int mensagem, int retorno)
    {
        return mensagem == 7 || (mensagem == 4 && retorno == 1);
    }

    public void Fechar()
    {
        if (!aberto) return;
        Motor.CloseDevice();
        aberto = false;
    }

    public void Dispose()
    {
        Fechar();
    }

    static class Motor
    {
        [DllImport("fpengine.dll", CallingConvention = CallingConvention.StdCall)] public static extern int OpenDevice(int porta, int parametro, int tipo);
        [DllImport("fpengine.dll", CallingConvention = CallingConvention.StdCall)] public static extern int LinkDevice(uint senha);
        [DllImport("fpengine.dll", CallingConvention = CallingConvention.StdCall)] public static extern int CloseDevice();
        [DllImport("fpengine.dll", CallingConvention = CallingConvention.StdCall)] public static extern int SetTimeOut(double segundos);
        [DllImport("fpengine.dll", CallingConvention = CallingConvention.StdCall)] public static extern void CaptureImage();
        [DllImport("fpengine.dll", CallingConvention = CallingConvention.StdCall)] public static extern int GetWorkMsg();
        [DllImport("fpengine.dll", CallingConvention = CallingConvention.StdCall)] public static extern int GetRetMsg();
        [DllImport("fpengine.dll", CallingConvention = CallingConvention.StdCall)] public static extern int GetRawImage(byte[] imagem, ref int tamanho);
        [DllImport("fpengine.dll", CallingConvention = CallingConvention.StdCall)] public static extern int GetImageSize(ref int largura, ref int altura);
    }
}
}
