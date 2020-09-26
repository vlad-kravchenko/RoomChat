using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Client
{
    public partial class MainWindow : Window
    {
        HubConnection connection;
        IHubProxy myHub;
        bool IsFormClosed = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            await ConnectToSignalRServer();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            myHub.Invoke<string, string>("NewMessage", null, UserName.Text, Message.Text).Wait();
        }

        private void UserList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string interlocutor = UserList.SelectedItem.ToString();
            if (interlocutor == UserName.Text) return;
            myHub.Invoke<string, string>("NewRoom", null, UserName.Text, interlocutor).Wait();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IsFormClosed = true;
            if (connection != null)
                connection.Stop();
        }

        private async Task<bool> ConnectToSignalRServer()
        {
            bool connected = false;
            try
            {
                connection = new HubConnection("http://localhost:8080/signalr");
                myHub = connection.CreateHubProxy("RoomChatHub");
                SetupHub();
                connection.Headers["UserName"] = UserName.Text;
                await connection.Start();
                if (connection.State == ConnectionState.Connected)
                {
                    connected = true;
                    connection.Closed += Connection_Closed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            return connected;
        }

        private void SetupHub()
        {
            myHub.On<List<string>>("broadcastUsers", (users) =>
            {
                Dispatcher.Invoke(() =>
                {
                    UserList.Items.Clear();
                    foreach (var user in users)
                        UserList.Items.Add(user);
                });
            });
            myHub.On<string, string>("newMessageReceived", (user, message) =>
            {
                string date = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + ":" + DateTime.Now.Second;
                string result = date + " " + user + ": " + message + Environment.NewLine;
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    Chat.AppendText(result);
                }));
            });
            myHub.On<string>("newRoomCreated", (creator) =>
            {
                string date = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + ":" + DateTime.Now.Second;
                string result = date + " " + creator + " started conversation" + Environment.NewLine + Environment.NewLine;
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    Chat.Document.Blocks.Clear();
                    Chat.AppendText(result);
                }));
            });
            myHub.On("roomClosed", () =>
            {
                string date = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + ":" + DateTime.Now.Second;
                string result = date + " Interlocutor left the room" + Environment.NewLine + Environment.NewLine;
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    Chat.AppendText(result);
                }));
            });
        }

        private async void Connection_Closed()
        {
            if (!IsFormClosed)
            {
                TimeSpan retryDuration = TimeSpan.FromSeconds(30);
                DateTime retryTill = DateTime.UtcNow.Add(retryDuration);

                while (DateTime.UtcNow < retryTill)
                {
                    bool connected = await ConnectToSignalRServer();
                    if (connected)
                    {
                        UserName.IsEnabled = false;
                        return;
                    }
                }
                MessageBox.Show("Connection closed");
            }
        }
    }
}