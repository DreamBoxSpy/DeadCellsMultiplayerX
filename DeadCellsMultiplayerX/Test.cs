using DeadCellsMultiplayerX.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX
{
    internal class Test
    {
       
        public static async void Start()
        {
            await ClientMain.Instance.StartHost("127.0.0.1", 12345);

            var guest = ClientMain.Instance.CurrentGuestClient!;
            guest.SetReady(true);

            await Task.Delay(500);

            await ClientMain.Instance.CurrentHostClient!.StartGame();
        }

        public static async void StartClient()
        {
            await ClientMain.Instance.StartGuest("127.0.0.1", 12345);
            var guest = ClientMain.Instance.CurrentGuestClient!;
            guest.SetReady(true);
        }
    }
}
