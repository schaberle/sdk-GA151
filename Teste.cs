using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
    [DllImport("fpengine.dll", CallingConvention = CallingConvention.StdCall)] public static extern int CreateTemplate(byte[] digital, byte[] molde);
    [DllImport("fpengine.dll", CallingConvention = CallingConvention.StdCall)] public static extern int MatchTemplateOne(byte[] atual, byte[] referencia, int tamanho);
}

sealed class TesteLeitor : Form
{
    const int Limiar = 80;
    readonly PictureBox imagem = new PictureBox();
    readonly Label estado = new Label();
    readonly Label resultado = new Label();
    readonly Button gravar = new Button();
    readonly Button validar = new Button();
    readonly Button limpar = new Button();
    readonly Timer relogio = new Timer();
    byte[] referencia;
    byte[] quadro;
    bool aberto, lendo, modoGravar;
    int ultima = -1, ultimoRetorno = -1;
    DateTime limite;

    public TesteLeitor()
    {
        Text = "Teste de leitura e validação";
        ClientSize = new Size(640, 760);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 11);
        BackColor = Color.White;
        var titulo = new Label { Text = "Leitor GA151", Location = new Point(24, 16), AutoSize = true, Font = new Font("Segoe UI", 20, FontStyle.Bold) };
        var ajuda = new Label { Text = "1. Grave a referência.  2. Valide o mesmo dedo. Outro dedo deve falhar.", Location = new Point(24, 58), Size = new Size(590, 28) };
        imagem.SetBounds(128, 100, 384, 432);
        imagem.BorderStyle = BorderStyle.FixedSingle;
        imagem.BackColor = Color.FromArgb(248, 249, 250);
        imagem.SizeMode = PictureBoxSizeMode.Zoom;
        gravar.Text = "Gravar referência"; gravar.SetBounds(24, 550, 180, 40);
        validar.Text = "Validar"; validar.SetBounds(214, 550, 140, 40); validar.Enabled = false;
        limpar.Text = "Apagar referência"; limpar.SetBounds(364, 550, 180, 40);
        resultado.SetBounds(24, 604, 590, 52); resultado.Font = new Font("Segoe UI", 28, FontStyle.Bold); resultado.Text = "";
        estado.SetBounds(24, 664, 590, 72); estado.Text = "Conectando...";
        Controls.AddRange(new Control[] { titulo, ajuda, imagem, gravar, validar, limpar, resultado, estado });
        gravar.Click += delegate { Iniciar(true); };
        validar.Click += delegate { Iniciar(false); };
        limpar.Click += delegate { Apagar(); };
        relogio.Interval = 40;
        relogio.Tick += delegate { Ler(); };
        Shown += delegate { Conectar(); };
        FormClosed += delegate { relogio.Stop(); if (aberto) Motor.CloseDevice(); if (imagem.Image != null) imagem.Image.Dispose(); };
    }

    void Conectar()
    {
        if (Motor.OpenDevice(0, 0, 0) != 1) { estado.Text = "Leitor indisponível. Feche outro programa que use o sensor e abra de novo."; return; }
        if (Motor.LinkDevice(0) != 1) { Motor.CloseDevice(); estado.Text = "O leitor não respondeu."; return; }
        Motor.SetTimeOut(0);
        aberto = true;
        estado.Text = "Leitor conectado. Apoie o dedo e clique em Gravar referência.";
    }

    void Apagar()
    {
        if (lendo) return;
        referencia = null;
        validar.Enabled = false;
        resultado.Text = "";
        resultado.ForeColor = Color.Black;
        estado.Text = "Referência apagada. Grave outra para repetir o teste.";
    }

    void Iniciar(bool gravacao)
    {
        if (!aberto || lendo) return;
        if (!gravacao && referencia == null) return;
        modoGravar = gravacao;
        lendo = true;
        quadro = null;
        ultima = -1;
        ultimoRetorno = -1;
        limite = DateTime.UtcNow.AddSeconds(50);
        gravar.Enabled = false;
        validar.Enabled = false;
        limpar.Enabled = false;
        resultado.Text = "";
        estado.Text = gravacao ? "Apoie o dedo no centro e segure." : "Apoie o dedo no centro para validar.";
        Motor.SetTimeOut(45);
        Motor.CaptureImage();
        relogio.Start();
    }

    void Parar(string texto, string conclusao, Color cor)
    {
        relogio.Stop();
        lendo = false;
        gravar.Enabled = aberto;
        validar.Enabled = aberto && referencia != null;
        limpar.Enabled = true;
        estado.Text = texto;
        resultado.Text = conclusao;
        resultado.ForeColor = cor;
    }

    void Ler()
    {
        if (DateTime.UtcNow > limite)
        {
            if (quadro != null) ConcluirImagem();
            else Parar("Tempo esgotado. Apoie o dedo no centro e segure.", "", Color.Black);
            return;
        }
        int mensagem = Motor.GetWorkMsg();
        int retorno = Motor.GetRetMsg();
        if (mensagem == ultima && retorno == ultimoRetorno) return;
        ultima = mensagem;
        ultimoRetorno = retorno;
        if (mensagem == 1) { aberto = false; Motor.CloseDevice(); Parar("O leitor foi desconectado.", "", Color.Black); return; }
        if (mensagem == 2) estado.Text = "Dedo detectado. Segure até a imagem aparecer.";
        if (ImagemPronta(mensagem, retorno))
        {
            if (!MostrarImagem()) return;
            if (mensagem == 4 && retorno == 1) ConcluirImagem();
        }
        else if (mensagem == 4 && retorno == 0) Parar("A leitura falhou. Centralize o dedo e tente de novo.", "", Color.Black);
        else if (mensagem == 8) Parar("Tempo esgotado. Apoie o dedo no centro e segure.", "", Color.Black);
    }

    bool MostrarImagem()
    {
        int largura = 0, altura = 0;
        Motor.GetImageSize(ref largura, ref altura);
        byte[] bruto = new byte[1024 * 1024];
        int tamanho = bruto.Length;
        if (Motor.GetRawImage(bruto, ref tamanho) != 1 || largura != 256 || altura != 288 || tamanho < 256 * 288) return false;
        quadro = new byte[256 * 288];
        Array.Copy(bruto, quadro, quadro.Length);
        estado.Text = "Imagem recebida.";
        Bitmap bitmap = new Bitmap(largura, altura, PixelFormat.Format24bppRgb);
        BitmapData pixels = bitmap.LockBits(new Rectangle(0, 0, largura, altura), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        try
        {
            byte[] rgb = new byte[Math.Abs(pixels.Stride) * altura];
            for (int y = 0; y < altura; y++)
                for (int x = 0; x < largura; x++)
                {
                    byte cinza = bruto[y * largura + x];
                    int p = y * pixels.Stride + x * 3;
                    rgb[p] = cinza; rgb[p + 1] = cinza; rgb[p + 2] = cinza;
                }
            Marshal.Copy(rgb, 0, pixels.Scan0, rgb.Length);
        }
        finally { bitmap.UnlockBits(pixels); }
        Image anterior = imagem.Image;
        imagem.Image = bitmap;
        if (anterior != null) anterior.Dispose();
        return true;
    }

    void ConcluirImagem()
    {
        byte[] molde = new byte[512];
        // shortcut: CreateTemplate devolve 0 quando extrai, e ainda apaga o quadro.
        if (quadro == null || Motor.CreateTemplate(quadro, molde) != 0 || MoldeVazio(molde))
        {
            Parar("A foto apareceu, mas a comparação não saiu desta imagem.", "Foto lida", Color.FromArgb(0, 102, 204));
            return;
        }
        if (modoGravar || referencia == null)
        {
            referencia = molde;
            Parar("Referência gravada. Retire o dedo e clique em Validar.", "Referência pronta", Color.FromArgb(0, 102, 204));
            return;
        }
        int pontuacao = Motor.MatchTemplateOne(molde, referencia, referencia.Length);
        if (Confere(pontuacao)) Parar("Mesma digital. Pontuação " + pontuacao + ". Limiar deste teste: " + Limiar + ".", "Confere", Color.FromArgb(20, 120, 50));
        else Parar("Digital diferente. Pontuação " + pontuacao + ". Limiar deste teste: " + Limiar + ".", "Não confere", Color.FromArgb(160, 40, 40));
    }

    // shortcut: 80 é o corte do helper Lirox desta DLL, não uma calibração do sensor.
    public static bool ImagemPronta(int mensagem, int retorno)
    {
        return mensagem == 7 || (mensagem == 4 && retorno == 1);
    }

    public static bool MoldeVazio(byte[] molde)
    {
        for (int i = 0; i < molde.Length; i++) if (molde[i] != 0) return false;
        return true;
    }

    public static bool Confere(int pontuacao) { return pontuacao >= Limiar; }

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--verificar")
        {
            byte[] vazio = new byte[4];
            byte[] cheio = new byte[] { 0, 1 };
            return Confere(80) && Confere(100) && !Confere(79) && ImagemPronta(7, 0) && ImagemPronta(4, 1) && !ImagemPronta(4, 0) && MoldeVazio(vazio) && !MoldeVazio(cheio) ? 0 : 1;
        }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TesteLeitor());
        return 0;
    }
}
