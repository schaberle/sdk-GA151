# sdk-GA151

Acesso ao leitor Multilaser GA151 no Windows. O aparelho entrega um quadro cru de 256×288. O cadastro e a comparação ficam no sistema que receber esses bytes.

Consulta feita nesta unidade em 25/10/2026: abertura, nome e tamanho confirmados. Cartão, flash, Bluetooth, Wi-Fi e tela responderam ausentes.

## Aparelho

- USB `VID_2009`, `PID_7638`, serial `202730041341`.
- A biblioteca, com o leitor aberto, responde o nome `RT1011`.
- `fpengine.dll` 8.11.0.2 (LIROX FAP10 Fingerprint Device Interface), mais `fpcore32.dll`, `fpspeed32.dll` e `nbis32.dll`. Essas quatro DLLs estão em `dlls\`. São bibliotecas de terceiros, necessárias para abrir o sensor.
- Processo de 32 bits. A convenção é `stdcall`.
- A mesma USB também aparece como armazenamento. Essa interface não captura digital.

## Compilar

No PowerShell, na pasta deste repositório:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

O compilador é o `csc.exe` do .NET Framework 4, com `/platform:x86`. A saída fica em `bin\`: `Leitor.dll`, `Capturar.exe`, `Teste.exe` e as quatro DLLs.

## Capturar uma imagem

Feche qualquer outro programa que esteja usando o sensor. Na pasta `bin`:

```powershell
.\Capturar.exe
.\Capturar.exe C:\temp\digital.raw
.\Capturar.exe --verificar
```

Sem argumento, grava `captura.raw` na pasta atual. Apoie o dedo no sensor. O programa espera até 60 segundos. `--verificar` só testa a regra de imagem pronta, sem abrir o USB.

## Teste na tela

`bin\Teste.exe` abre a janela de leitura e validação. Grave a referência, retire o dedo e valide o mesmo dedo. A referência fica só na memória e some ao fechar a janela. Feche outro programa que esteja usando o sensor antes de abrir.

## Uso no código

O programa que referencia `Leitor.dll` também precisa ser x86 e ficar ao lado das quatro DLLs.

```csharp
using (Leitor leitor = new Leitor())
{
    leitor.Abrir();
    byte[] quadro = leitor.Capturar(TimeSpan.FromSeconds(60));
}
```

`Abrir` chama `OpenDevice(0, 0, 0)` e `LinkDevice(0)`. Os dois precisam devolver 1. `Capturar` devolve 73728 bytes: 8 bits por pixel, sem cabeçalho, 256 bytes por linha, 288 linhas. `Fechar` ou `Dispose` chama `CloseDevice`. Um segundo processo não abre o mesmo USB.

O sensor permanece ligado. `SetTimeOut(0)` impede a DLL de encerrar a espera sozinha. A imagem está pronta quando `GetWorkMsg` vale 7, ou vale 4 e `GetRetMsg` vale 1. Mensagem 1 significa que o leitor foi desconectado.

## O que não está neste aparelho

Não há memória de digitais, cartão, Bluetooth, Wi-Fi nem tela. `GetDeviceSnStr` não devolve o serial. O serial do hardware é o da USB, `202730041341`. O pacote `SynoAPIEx.dll` não concluiu a leitura dos parâmetros nesta unidade.
