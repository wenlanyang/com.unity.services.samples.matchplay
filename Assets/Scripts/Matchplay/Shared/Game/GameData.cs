using System;
using System.Text;
using UnityEngine;

namespace Matchplay.Shared
{
    [Flags]
    public enum Map
    {
        None = 0,
        Lab = 1,
        Space = 2
    }

    [Flags]
    public enum GameMode
    {
        None = 0,
        Staring = 1,
        Meditating = 2
    }

    public enum GameQueue
    {
        Casual,
        Competetive
    }

    /// <summary>
    /// Wrapping the userData into a class that will callback to listeners when changed, for example, UI.
    /// </summary>
    public class ObservableUser
    {
        public ObservableUser()
        {
            Data = new UserData("player", "", 0, new GameInfo());
        }

        public UserData Data { get; }

        public string Name
        {
            get => Data.userName;
            set
            {
                Data.userName = value;
                onNameChanged?.Invoke(Data.userName);
            }
        }

        public Action<string> onNameChanged;

        public string AuthId
        {
            get => Data.clientAuthId;
            set
            {
                Data.clientAuthId = value;
                onAuthChanged?.Invoke(Data.clientAuthId);
            }
        }

        public Action<string> onAuthChanged;

        public ulong SetNetworkID
        {
            get => Data.networkId;
            set => Data.networkId = value;
        }

        public Map Map
        {
            get => Data.gameInfo.map;
            set
            {
                Data.gameInfo.map = value;
                onMapChanged?.Invoke(Data.gameInfo.map);
            }
        }

        public Action<Map> onMapChanged;

        public GameMode Mode
        {
            get => Data.gameInfo.gameMode;
            set
            {
                Data.gameInfo.gameMode = value;
                onModeChanged?.Invoke(Data.gameInfo.gameMode);
            }
        }

        public Action<GameMode> onModeChanged;

        public GameQueue Queue
        {
            get => Data.gameInfo.gameQueue;
            set
            {
                Data.gameInfo.gameQueue = value;
                onQueueChanged?.Invoke(Data.gameInfo.gameQueue);
            }
        }

        public Action<GameQueue> onQueueChanged;

        public override string ToString()
        {
            var userData = new StringBuilder("ObservableUser: ");
            userData.AppendLine($"- {Data}");
            return userData.ToString();
        }
    }

    /// <summary>
    /// All the data we would want to pass across the network.
    /// </summary>
    [Serializable]
    public class UserData
    {
        public string userName; //name of the player
        public string clientAuthId; //Auth Player ID
        public ulong networkId;
        public GameInfo gameInfo; //The game info the player thought he was joining with

        public UserData(string userName, string clientAuthId, ulong networkId, GameInfo gameInfo)
        {
            this.userName = userName;
            this.clientAuthId = clientAuthId;
            this.networkId = networkId;
            this.gameInfo = gameInfo;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("UserData: ");
            sb.AppendLine($"- userName:       {userName}");
            sb.AppendLine($"- clientAuthId:   {clientAuthId}");
            sb.AppendLine($"- {gameInfo}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Subset of information that pertains to the setup of a game
    /// </summary>
    [Serializable]
    public class GameInfo
    {
        public Map map;
        public GameMode gameMode;
        public GameQueue gameQueue;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("GameInfo: ");
            sb.AppendLine($"- map:        {map}");
            sb.AppendLine($"- gameMode:   {gameMode}");
            sb.AppendLine($"- GameQueue:  {gameQueue}");
            return sb.ToString();
        }

        /// <summary>
        /// Convert queue enums to ticket queue name
        /// (Same as your queue name in the matchmaker dashboard)
        /// </summary>
        public string MultiplayQueue()
        {
            return gameQueue switch
            {
                GameQueue.Casual => "casual-queue",
                GameQueue.Competetive => "competetive-queue",
                _ => "casual-queue"
            };
        }
    }
}
