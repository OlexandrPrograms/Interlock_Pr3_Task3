using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Interlock_Pr3
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            int price = 1000;
            int current = price;
            int leaderId = -1;
            int isClosed = 0;

            object auctionLock = new object();
            List<int> history = new List<int>();

            Console.WriteLine($"Початок аукціону!");

            int testBid = current;
            int incrementVal = 100;
            int initial, computed;
            do
            {
                initial = testBid;
                computed = initial + incrementVal;
            }
            while (Interlocked.CompareExchange(ref testBid, computed, initial) != initial);
            Console.WriteLine($"Успішно оновлено ціну через Interlocked: {testBid}");


            Thread[] threads = new Thread[8];
            for (int i = 0; i < threads.Length; i++)
            {
                int bidderId = i + 1;
                threads[i] = new Thread(() =>
                {
                    Random rand = new Random(Guid.NewGuid().GetHashCode());

                    while (isClosed == 0)
                    {
                        Thread.Sleep(rand.Next(50, 200));
                        if (isClosed != 0) break;

                        if (rand.NextDouble() < 0.6)
                        {
                            int increment = rand.Next(50, 501);

                            lock (auctionLock)
                            {
                                if (isClosed != 0) break;

                                int proposedBid = current + increment;
                                if (proposedBid > current)
                                {
                                    current = proposedBid;
                                    leaderId = bidderId;
                                    history.Add(proposedBid);
                                    Console.WriteLine($"Учасник #{bidderId} зробив ставку: {proposedBid} грн");
                                }
                            }
                        }
                    }
                });
                threads[i].Start();
            }


            Thread hammerThread = new Thread(() =>
            {
                Thread.Sleep(3000);
                Console.WriteLine("\n3 секунди вичерпано!");

                if (Interlocked.Exchange(ref isClosed, 1) == 0)
                {
                    lock (auctionLock)
                    {
                        Console.WriteLine("==========================================");
                        Console.WriteLine("ПРОДАНО!");
                        if (leaderId != -1)
                        {
                            Console.WriteLine($"Переможець: Учасник #{leaderId}");
                            Console.WriteLine($"Фінальна ціна: {current} грн");
                        }
                        else
                            Console.WriteLine("Ніхто не зробив ставок");
                        Console.WriteLine("==========================================");
                    }
                }
            });
            hammerThread.Start();


            foreach (var thread in threads)
                thread.Join();
            hammerThread.Join();


            Console.WriteLine("\n--- Перевірка журналу ставок ---");
            bool isStrictlyIncreasing = true;
            for (int i = 1; i < history.Count; i++)
            {
                if (history[i] <= history[i - 1])
                {
                    isStrictlyIncreasing = false;
                    break;
                }
            }

            Console.WriteLine($"Всього прийнято ставок у журналі: {history.Count}");
            Console.WriteLine($"Ставки строго зростають: {(isStrictlyIncreasing ? "ТАК" : "НІ")}");

            if (history.Count > 0)
            {
                int lastEntry = history[history.Count - 1];
                bool matchesFinal = (lastEntry == current);
                Console.WriteLine($"Останній запис у журналі: {lastEntry} грн");
                Console.WriteLine($"Збігається з фінальною ціною: {(matchesFinal ? "ТАК" : "НІ")}");
            }
            else
                Console.WriteLine("Журнал порожній");
        }
    }
}
