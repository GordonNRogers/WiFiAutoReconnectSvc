﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WiFiAutoReconnectLib;

namespace WiFiAutoReconnect_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Test1();
            Test2();
            Test3();
            Test4();
        }

        private static void Test1()
        {
            // expected behavior:  thread will run once, set completedEvent and then be shut cleanly down by Stop().
            WiFi_Connector connector = new WiFi_Connector();
            using (Logger _logFile = Logger.CreateLogger())
            {
                _logFile.LogWithTimestamp("Start Test 1", Logger.LogLevel.DIAGNOSTIC);
                AutoResetEvent completedEvent = new AutoResetEvent(false);
                TimeSpan timeout = new TimeSpan(0, 2, 0);

                // set the OnComplete event to an anonymous function that sets completedEvent
                connector.OnComplete += new EventHandler((o, e) => { completedEvent.Set(); });
                connector.Start();
                // wait for the completed event
                completedEvent.WaitOne(timeout);
                connector.Stop();
                _logFile.LogWithTimestamp("End Test 1", Logger.LogLevel.DIAGNOSTIC);
                _logFile.LogWithTimestamp("", Logger.LogLevel.INFO);
            }
        }

        private static void Test2()
        {
            // expected behavior:  thread will run once, completedEvent will keep it busy so Stop() times out and has to abort the thread.
            WiFi_Connector connector = new WiFi_Connector();
            using (Logger _logFile = Logger.CreateLogger())
            {
                _logFile.LogWithTimestamp("Start Test 2", Logger.LogLevel.DIAGNOSTIC);

                // use the OnComplete event handler to keep the thread busy long enough to exceed the timeout passed to Thread.Join()
                connector.OnComplete += new EventHandler((o, e) =>
                {
                    Thread.Sleep(1000 * 60 * 5);
                });
                connector.Start();
                connector.Stop();
                _logFile.LogWithTimestamp("End Test 2", Logger.LogLevel.DIAGNOSTIC);
                _logFile.LogWithTimestamp("", Logger.LogLevel.INFO);
            }
        }

        private static void Test3()
        {
            // expected behavior:  thread will run multiple times and exit cleanly when Stop() is called.
            // fireCount should be > 0.
            WiFi_Connector connector = new WiFi_Connector();
            using (Logger _logFile = Logger.CreateLogger())
            {
                _logFile.LogWithTimestamp("Start Test 3", Logger.LogLevel.DIAGNOSTIC);
                AutoResetEvent completedEvent = new AutoResetEvent(false);
                int fireCount = 0;

                connector.OnComplete += new EventHandler((o, e) => { fireCount++; });

                connector.Start();
                while (fireCount < 3)
                {
                    Thread.Sleep(500);
                }
                connector.Stop();
                Console.WriteLine("Fire Count: " + fireCount);
                _logFile.LogWithTimestamp("End Test 3", Logger.LogLevel.DIAGNOSTIC);
                _logFile.LogWithTimestamp("", Logger.LogLevel.INFO);
            }
        }

        private static void Test4()
        {
            // a longer duration test to get a better feel of cpu usage
            WiFi_Connector connector = new WiFi_Connector();
            using (Logger _logFile = Logger.CreateLogger())
            {
                TimeSpan testLength = new TimeSpan(0, 10, 0);
                DateTime testEndTime = DateTime.Now + testLength;
                _logFile.LogWithTimestamp("Start Test 4", Logger.LogLevel.DIAGNOSTIC);
                AutoResetEvent completedEvent = new AutoResetEvent(false);
                int fireCount = 0;

                connector.OnComplete += new EventHandler((o, e) => { fireCount++; });

                connector.Start();
                while (DateTime.Now <= testEndTime)
                {
                    Thread.Sleep(500);
                }
                connector.Stop();
                Console.WriteLine("Fire Count: " + fireCount);
                _logFile.LogWithTimestamp("End Test 3", Logger.LogLevel.DIAGNOSTIC);
                _logFile.LogWithTimestamp("", Logger.LogLevel.INFO);
            }
        }

    }
}
