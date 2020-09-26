using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;

namespace Server
{
    class Program
    {
        public static List<User> ChatUsers { get; set; }
        public static List<Room> ChatRooms { get; set; }

        static void Main(string[] args)
        {
            ChatUsers = new List<User>();
            ChatRooms = new List<Room>();
            string url = "http://localhost:8080";
            using (WebApp.Start(url))
            {
                Console.WriteLine("Server running on {0}", url);
                Console.ReadLine();
            }
        }
    }
}