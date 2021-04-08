﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using UGCS.Sdk.Protocol;
using UGCS.Sdk.Protocol.Encoding;
using UGCS.Sdk.Tasks;
using TcpClient = UGCS.Sdk.Protocol.TcpClient;
using TcpClientt = System.Net.Sockets;

namespace DirectVehicleControl
{
    class Program
    {
        static void Main(string[] args)
        {
            //Connect
            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect("localhost", 3334);
            MessageSender messageSender = new MessageSender(tcpClient.Session);
            MessageReceiver messageReceiver = new MessageReceiver(tcpClient.Session);
            MessageExecutor messageExecutor =
                new MessageExecutor(messageSender, messageReceiver, new InstantTaskScheduler());
            messageExecutor.Configuration.DefaultTimeout = 10000;
            var notificationListener = new NotificationListener();
            messageReceiver.AddListener(-1, notificationListener);

            //auth
            AuthorizeHciRequest request = new AuthorizeHciRequest();
            request.ClientId = -1;
            request.Locale = "en-US";
            var future = messageExecutor.Submit<AuthorizeHciResponse>(request);
            future.Wait();
            AuthorizeHciResponse AuthorizeHciResponse = future.Value;
            int clientId = AuthorizeHciResponse.ClientId;
            System.Console.WriteLine("AuthorizeHciResponse precessed");

            //login
            LoginRequest loginRequest = new LoginRequest();
            loginRequest.UserLogin = "admin";
            loginRequest.UserPassword = "admin";
            loginRequest.ClientId = clientId;
            var loginResponcetask = messageExecutor.Submit<LoginResponse>(loginRequest);
            loginResponcetask.Wait();

            // Id of the emu-copter is 2
            var vehicleToControl = new Vehicle {Id = 3};

            TcpClientt.TcpListener server = new TcpClientt.TcpListener(IPAddress.Any, 8080);
            server.Start(); // run server
            byte[] ok = new byte[100];
            ok = Encoding.Default.GetBytes("ok");
            while (true) // бесконечный цикл обслуживания клиентов
            {
                TcpClientt.TcpClient client = server.AcceptTcpClient(); // ожидаем подключение клиента
                TcpClientt.NetworkStream ns = client.GetStream(); // для получения и отправки сообщений
                // byte[] ok = new byte[100]; 
                // ok = Encoding.Default.GetBytes("ok"); TODO Connected
                //
                // ns.Write(ok, 0, ok.Length);
                while (client.Connected) // пока клиент подключен, ждем приходящие сообщения
                {
                    byte[] msg = new byte[100]; // готовим место для принятия сообщения
                    int count = ns.Read(msg, 0, msg.Length); // читаем сообщение от клиента
                    Console.Write(Encoding.Default.GetString(msg, 0,
                        count)); // выводим на экран полученное сообщение в виде строки
                    string allMessage = Encoding.Default.GetString(msg);
                    string result = allMessage.Substring(0, count - 1);
                    var commandName = result.ToString().Split(":")[0];


                    switch (commandName)
                    {
                        case "takeoff_command":
                        {
                            Console.Write("got command: {0}", commandName);

                            SendCommandRequest takeoff = new SendCommandRequest
                            {
                                ClientId = clientId,
                                Command = new Command
                                {
                                    Code = "takeoff_command",
                                    Subsystem = Subsystem.S_FLIGHT_CONTROLLER,
                                    Silent = true,
                                    ResultIndifferent = true
                                }
                            };
                            takeoff.Vehicles.Add(vehicleToControl);
                            var takeoffCmd = messageExecutor.Submit<SendCommandResponse>(takeoff);
                            takeoffCmd.Wait();
                            Thread.Sleep(5000);
                            ns.Write(ok, 0, ok.Length);
                            break;
                        }
                        case "direct_vehicle_control":
                        {
                            Console.Write("got command: {0}", commandName);
                            var commandArgs = result.Split(":")[1];
                            Console.Write("args of command: {0}", commandArgs);
                            // Vehicle control in joystick mode
                            SendCommandRequest vehicleJoystickControl = new SendCommandRequest
                            {
                                ClientId = clientId,
                                Command = new UGCS.Sdk.Protocol.Encoding.Command
                                {
                                    Code = "direct_vehicle_control",
                                    Subsystem = Subsystem.S_FLIGHT_CONTROLLER,
                                    Silent = true,
                                    ResultIndifferent = true
                                }
                            };


                            vehicleJoystickControl.Vehicles.Add(vehicleToControl);

                            List<CommandArgument> listJoystickCommands = new List<CommandArgument>();
                            var directionCommand = commandArgs.ToString().Split(",")[0];
                            string commandValueStr = commandArgs.ToString().Split(",")[1];
                            double commandValue = double.Parse(commandValueStr,
                                System.Globalization.CultureInfo.InvariantCulture);

                            switch (directionCommand)
                            {
                                case "roll":
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "roll",
                                        Value = new Value() {DoubleValue = commandValue}
                                    });
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "pitch",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "yaw",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "throttle",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    break;

                                case "pitch":
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "pitch",
                                        Value = new Value() {DoubleValue = commandValue}
                                    });
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "roll",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "yaw",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "throttle",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    break;

                                case "throttle":
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "throttle",
                                        Value = new Value() {DoubleValue = commandValue}
                                    });
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "pitch",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "roll",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "yaw",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    break;

                                case "yaw":
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "yaw",
                                        Value = new Value() {DoubleValue = commandValue}
                                    });
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "pitch",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "roll",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listJoystickCommands.Add(new CommandArgument
                                    {
                                        Code = "throttle",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    break;
                            }


                            vehicleJoystickControl.Command.Arguments.AddRange(listJoystickCommands);
                            var sendJoystickCommandResponse =
                                messageExecutor.Submit<SendCommandResponse>(vehicleJoystickControl);
                            sendJoystickCommandResponse.Wait();
                            System.Console.WriteLine("Was sent {0}", commandValue);

                            Thread.Sleep(2000);
                            ns.Write(ok, 0, ok.Length);
                            break;
                        }
                        case "payload_control":
                        {
                            Console.Write("got command: {0}", commandName);
                            var command_args = result.ToString().Split(":")[1];
                            Console.Write("args of command: {0}", command_args);
                            // Vehicle control in joystick mode
                            SendCommandRequest vehiclePayloadCommandRequest = new SendCommandRequest
                            {
                                ClientId = clientId,
                                Command = new UGCS.Sdk.Protocol.Encoding.Command
                                {
                                    Code = "payload_control",
                                    Subsystem = Subsystem.S_FLIGHT_CONTROLLER,
                                    Silent = false,
                                    ResultIndifferent = false
                                }
                            };
                            vehiclePayloadCommandRequest.Vehicles.Add(vehicleToControl);
                            List<CommandArgument> listPayloadCommands = new List<CommandArgument>();
                            var direction_command = command_args.ToString().Split(",")[0];
                            string command_value_str = command_args.ToString().Split(",")[1];
                            double command_value = double.Parse(command_value_str,
                                System.Globalization.CultureInfo.InvariantCulture);
                        
                            switch (direction_command)
                            {
                                case "tilt":
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "tilt",
                                        Value = new Value() {DoubleValue = command_value}
                                    });
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "roll",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "yaw",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "zoom_level",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    break;
                                case "roll":
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "roll",
                                        Value = new Value() {DoubleValue = command_value}
                                    });
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "tilt",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "yaw",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "zoom_level",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    break;
                                case "zoom_level":
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "zoom_level",
                                        Value = new Value() {DoubleValue = command_value}
                                    });
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "tilt",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "roll",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "yaw",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    break;
                                case "yaw":
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "yaw",
                                        Value = new Value() {DoubleValue = command_value}
                                    });
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "tilt",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "roll",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    listPayloadCommands.Add(new CommandArgument
                                    {
                                        Code = "zoom_level",
                                        Value = new Value() {DoubleValue = 0}
                                    });
                                    break;
                            }
                            vehiclePayloadCommandRequest.Command.Arguments
                                .AddRange(listPayloadCommands);
                            var sendPayloadCommandResponse =
                                messageExecutor.Submit<SendCommandResponse>(vehiclePayloadCommandRequest);
                            sendPayloadCommandResponse.Wait();
                            System.Console.WriteLine("Was sent {0}", command_value);
                            Thread.Sleep(2000);
                            ns.Write(ok, 0, ok.Length);
                            break;
                        }
                        case "land_command":
                        {
                            SendCommandRequest land = new SendCommandRequest
                            {
                                ClientId = clientId,
                                Command = new UGCS.Sdk.Protocol.Encoding.Command
                                {
                                    Code = "land_command",
                                    Subsystem = Subsystem.S_FLIGHT_CONTROLLER,
                                    Silent = false,
                                    ResultIndifferent = false
                                }
                            };
                            land.Vehicles.Add(vehicleToControl);
                            var landCmd = messageExecutor.Submit<SendCommandResponse>(land);
                            landCmd.Wait();
                            Thread.Sleep(5000);
                            ns.Write(ok, 0, ok.Length);
                            break;
                        }
                        case "joystick":
                        {
                            SendCommandRequest joystickModeCommand = new SendCommandRequest
                            {
                                ClientId = clientId,
                                Command = new UGCS.Sdk.Protocol.Encoding.Command
                                {
                                    Code = "joystick",
                                    Subsystem = Subsystem.S_FLIGHT_CONTROLLER,
                                    Silent = false,
                                    ResultIndifferent = false
                                }
                            };

                            joystickModeCommand.Vehicles.Add(vehicleToControl);
                            var joystickMode = messageExecutor.Submit<SendCommandResponse>(joystickModeCommand);
                            joystickMode.Wait();
                            ns.Write(ok, 0, ok.Length);
                            break;
                        }
                        case "manual":
                        {
                            break;
                        }
                    }
                }

                System.Console.ReadKey();
                tcpClient.Close();
                messageSender.Cancel();
                messageReceiver.Cancel();
                messageExecutor.Close();
                notificationListener.Dispose();
            }
        }
    }
}