using System;
using System.Threading;
using System.Threading.Tasks;

namespace NanoSockets.Receive
{
    class Program
    {
        //private const int PacketSize = 1380;
        private const int PacketSize = 60000;

        static void Main(string[] args)
        {
            UDP.Initialize();

            // Get a cancel source that cancels when the user presses CTRL+C.
            var userExitSource = GetUserConsoleCancellationSource();
            var cancelToken = userExitSource.Token;
            var throughput = new ThroughputCounter();
            // Start a background task to print throughput periodically.
            _ = PrintThroughput(throughput, cancelToken);

            Socket server = UDP.Create(256 * 1024, 256 * 1024);
            Address listenAddress = new Address
            {
                port = 55555
            };

            if (UDP.SetIP(ref listenAddress, "::0") == Status.OK)
                Console.WriteLine("Address set!");

            if (UDP.Bind(server, ref listenAddress) == 0)
                Console.WriteLine("Socket connected!");

            if (UDP.SetDontFragment(server) != Status.OK)
                Console.WriteLine("Don't fragment option error!");

            if (UDP.SetNonBlocking(server) != Status.OK)
                Console.WriteLine("Non-blocking option error!");

            byte[] buffer = new byte[PacketSize];

            while (true)
            {
                if (UDP.Poll(server, 15) > 0)
                {
                    int dataLength;
                    while ((dataLength = UDP.Receive(server, IntPtr.Zero, buffer, buffer.Length)) > 0)
                    {
                        throughput.Add(dataLength);
                    }
                }
            }

            UDP.Destroy(ref server);


            UDP.Deinitialize();
        }

        private static async Task PrintThroughput(ThroughputCounter counter, CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancelToken);

                var count = counter.SampleAndReset();

                var megabytes = count / 1024d / 1024d;

                double pps = count / PacketSize;

                Console.WriteLine("{0:0.00}MBps ({1:0.00}Mbps) - {2:0.00}pps", megabytes, megabytes * 8, pps);
            }
        }

        private static CancellationTokenSource GetUserConsoleCancellationSource()
        {
            var cancellationSource = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                cancellationSource.Cancel();
            };

            return cancellationSource;
        }
    }
}
