using System;


namespace GameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "GameServer";


            //Cange port to unused one
            Server.Start(6, 26950);

            Console.ReadKey();
        }
    }
}
