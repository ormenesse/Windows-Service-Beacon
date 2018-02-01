using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.IO;
using System.Net;

namespace SI_ARP
{
    static class ip_operations
    {
        public static List<IPAddress> recip(byte[] address, int indice, int max)
        {
            //Funcao recursiva de suport à IPs_rede
            List<IPAddress> ips_rede = new List<IPAddress>();
            if (indice == 3)
            {
                for (int i = address[indice]; i < 256; i++)
                {
                    address[indice] = (byte)i;
                    //Console.WriteLine("Adicionando IP's na lista");
                    ips_rede.Add(new IPAddress(address));
                }
            }
            else
            {
                if (address[indice] == 255 || address[indice] == max)
                {
                    //Console.WriteLine("Alterando Indice");
                    max = 255;
                    ips_rede.AddRange(recip(address, indice + 1, max));
                }
                else
                {
                    int numero = (int)address[indice] + 1;
                    address[indice] = (byte)numero;
                    //Console.WriteLine(new IPAddress(address));
                    ips_rede.AddRange(recip(address, indice + 1, max));
                }
            }
            return ips_rede;
        }
        public static List<IPAddress> IPs_Rede(this IPAddress address, IPAddress subnetmask)
        {
            // Metodo onde eu consigo todos os IP's de uma dada rede com sua subnet mask
            byte[] ipAddressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetmask.GetAddressBytes();
            //Console.WriteLine(subnetmask);
            List<IPAddress> ips_rede = new List<IPAddress>();
            if (ipAddressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Tamanho errado da mascara de subrede.");
            byte[] endereco = new byte[ipAddressBytes.Length];
            byte[] sub = new byte[4]; // primeiro IP da rede
            int vetor = 0;
            int dif = 255;
            for (int i = 0; i < subnetMaskBytes.Length; i++)
            {
                if ((int)subnetMaskBytes[i] == 255)
                {
                    sub[i] = ipAddressBytes[i];
                    vetor++;
                }
                else if ((int)subnetMaskBytes[i] > 0)
                {
                    dif = 255 - subnetMaskBytes[i];
                    int i1 = ipAddressBytes[i];
                    int i2 = subnetMaskBytes[i];
                    sub[i] = (byte)(i1 & i2);
                }
                else
                {
                    sub[i] = subnetMaskBytes[i];
                }
            }
            dif = dif + (int)sub[vetor];
            //agora eu preciso pegar todos os IP's da rede
            ips_rede.AddRange(recip(sub, vetor, dif)); //funcao recursiva para me retornar todos os IPs
            return ips_rede;
        }

        public static IPAddress GetBroadcastAddress(this IPAddress address, IPAddress subnetMask)
        {
            // Metodo utilizado para pegar o endereço Broadcast da minha rede.
            byte[] ipAddressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();
            if (ipAddressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");
            byte[] broadcastAddress = new byte[ipAddressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAddressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            return new IPAddress(broadcastAddress);
        }

        public static List<IPAddress> getmyips()
        {
            // Metodo utilizado para pegar todos os meus IP's
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            List<IPAddress> ips = new List<IPAddress>();
            foreach (IPAddress addr in localIPs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    ips.Add(addr);
                }
            }
            return ips;
        }

        public static IPAddress GetSubnetMask(IPAddress address)
        {
            //Funcao que pega todas as máscaras de subrede.
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (address.Equals(unicastIPAddressInformation.Address))
                        {
                            return unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException(string.Format("Can't find subnetmask for IP address '{0}'", address));
        }

        public static StreamReader ExecuteCommandLine(String file, String arguments = "")
        {
            // Processo para executar um programa na linha de comando.
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.FileName = file;
            startInfo.Arguments = arguments;

            Process process = Process.Start(startInfo);

            return process.StandardOutput;
        }

        public static Task<PingReply> PingAsync(string address)
        {
            //Envia Pings Assincronos.
            var tasks = new TaskCompletionSource<PingReply>();
            Ping ping = new Ping();
            ping.PingCompleted += (obj, sender) =>
            {
                tasks.SetResult(sender.Reply); //Resultado ficará como objeto ai.
            };
            ping.SendAsync(address, new object());
            return tasks.Task;
        }
    }
    public partial class Service1 : ServiceBase
    {
        public Service1(string[] args)
        {
            InitializeComponent();
            string eventSourceName = "SI_BEACON";
            string logName = "Application";
            
            eventLog1 = new EventLog();
            if (!EventLog.SourceExists(eventSourceName))
            {
                EventLog.CreateEventSource(eventSourceName, logName);
            }
            eventLog1.Source = eventSourceName;
            eventLog1.Log = logName;
        }
        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("Iniciando Serviço de coleta! Deseje-me sorte!", EventLogEntryType.Information, eventId);
            //TIMER
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 600000; // 600 seconds ou 10 minutos  
            timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("Mas que pena! Minha coleta termina agora...", EventLogEntryType.Information, eventId);
        }

        protected override void OnContinue()
        {
            eventLog1.WriteEntry("Continuando Serviço de coleta! Deseje-me sorte!", EventLogEntryType.Information, eventId);
            //TIMER
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 600000; // 600 seconds ou 10 minutos  
            timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }

        protected override void OnShutdown()
        {
            eventLog1.WriteEntry("Mas que pena! Minha coleta termina agora...", EventLogEntryType.Information, eventId);
        }

        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            // TODO: Insert monitoring activities here.  
            eventLog1.WriteEntry("Reiniciando a coleta...", EventLogEntryType.Information, eventId);


            //Funcao principal deste programa é atualizar tabela ARP da rede e se possível pingar e obter a resposta de todos os IP's.
            List<IPAddress> meus_ips = ip_operations.getmyips();
            List<IPAddress> subnets = new List<IPAddress>();
            //List<IPAddress> broadcast = new List<IPAddress>();
            for (int i = 0; i < meus_ips.Count; i++)
            {
                subnets.Add(ip_operations.GetSubnetMask(meus_ips.ElementAt<IPAddress>(i)));
                //broadcast.Add(GetBroadcastAddress(meus_ips.ElementAt<IPAddress>(i), subnets.ElementAt<IPAddress>(i))); //Console.WriteLine(subnets.ElementAt<IPAddress>(i)); Console.WriteLine(broadcast.ElementAt<IPAddress>(i));
            }
            List<IPAddress> IPs_na_rede = new List<IPAddress>();
            for (int i = 0; i < meus_ips.Count; i++)
            {
                //IP's Atualizados
                IPs_na_rede.AddRange(ip_operations.IPs_Rede(meus_ips.ElementAt<IPAddress>(i), subnets.ElementAt<IPAddress>(i)));
            }
            //Atualizando tabela ARP.
            for (int i = 0; i < IPs_na_rede.Count; i++)
            {
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP);
                IPEndPoint endPoint = new IPEndPoint(IPs_na_rede.ElementAt<IPAddress>(i), 11000);
                string text = "Hello! Has somewone some MAC Address I don't have?";
                byte[] send_buffer = Encoding.ASCII.GetBytes(text);

                sock.SendTo(send_buffer, endPoint);
            }

            var arpStream = ip_operations.ExecuteCommandLine("arp", "-a"); //Console.WriteLine(arpStream.ReadToEnd());

            eventLog1.WriteEntry("MacAddressCheck:" + arpStream.ReadToEnd(), EventLogEntryType.Information, 10, 1);
            //Enviando Ping
            List<Task<PingReply>> pingTasks = new List<Task<PingReply>>();
            for (int i = 0; i < IPs_na_rede.Count; i++)
            {
                pingTasks.Add(ip_operations.PingAsync(IPs_na_rede.ElementAt<IPAddress>(i).ToString()));
            }
            Task.WaitAll(pingTasks.ToArray());
            //Recebendo resultados Ping.
            String Resultado_pings = "Resultados de Resposta ICMP das máquinas conectadas.\n";
            foreach (var pingTask in pingTasks)
            {
                //pingTask.Result is whatever type T was declared in PingAsync
                if (pingTask.Result.Status.ToString() != "DestinationHostUnreachable")
                {
                    if (pingTask.Result.Status.ToString() != "TimedOut")
                    {
                        if (pingTask.Result.Address.ToString() != "0.0.0.0")
                            Resultado_pings = Resultado_pings + pingTask.Result.Address + " " + pingTask.Result.Status.ToString() + " " + pingTask.Result.RoundtripTime.ToString() + "ms.\n";
                    }
                }
            }
            eventLog1.WriteEntry(Resultado_pings, EventLogEntryType.Information, 10, 1);
        }
    }
}
