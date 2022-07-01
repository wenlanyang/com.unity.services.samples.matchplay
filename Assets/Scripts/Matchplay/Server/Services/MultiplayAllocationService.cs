using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Matchplay.Server
{
    public class MultiplayAllocationService : IDisposable
    {
        IMultiplayService m_MultiplayService;
        MultiplayEventCallbacks m_Servercallbacks;
        IServerCheckManager m_ServerCheckManager;
        IServerEvents m_ServerEvents;
        string m_AllocationId;
        bool m_LocalServerValuesChanged = false;
        CancellationTokenSource m_ServerCheckCancel;

        const string k_PayloadProxyUrl = "http://localhost:8086";

        public MultiplayAllocationService()
        {
            try
            {
                m_MultiplayService = MultiplayService.Instance;
                m_ServerCheckCancel = new CancellationTokenSource();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error creating Multiplay allocation service.\n{ex}");
            }
        }

        /// <summary>
        /// Should be wrapped in a timeout function
        /// </summary>
        public async Task<MatchmakerAllocationPayload> SubscribeAndAwaitMatchmakerAllocation()
        {
            if (m_MultiplayService == null)
                return null;
            m_AllocationId = null;
            m_Servercallbacks = new MultiplayEventCallbacks();
            m_Servercallbacks.Allocate += OnMultiplayAllocation;
            m_ServerEvents = await m_MultiplayService.SubscribeToServerEventsAsync(m_Servercallbacks);

            var allocationID = await AwaitAllocationID();
            var mmPayload = await GetMatchmakerAllocationPayloadAsync(allocationID);

            return mmPayload;
        }

        //The networked server is our source of truth for what is going on, so we update our multiplay check server with values from there.
        public async Task BeginServerCheck()
        {
            if (m_MultiplayService == null)
                return;
            m_ServerCheckManager = await m_MultiplayService.StartServerQueryHandlerAsync((ushort)10,
                "", "", "0", "");

#pragma warning disable 4014
            ServerCheckLoop(m_ServerCheckCancel.Token);
#pragma warning restore 4014
        }

        public void SetServerName(string name)
        {
            m_ServerCheckManager.ServerName = name;
            m_LocalServerValuesChanged = true;
        }

        public void SetBuildID(string id)
        {
            m_ServerCheckManager.BuildId = id;
            m_LocalServerValuesChanged = true;
        }

        public void SetMaxPlayers(ushort players)
        {
            m_ServerCheckManager.MaxPlayers = players;
        }

        public void SetPlayerCount(ushort count)
        {
            m_ServerCheckManager.CurrentPlayers = count;
            m_LocalServerValuesChanged = true;
        }

        public void AddPlayer()
        {
            m_ServerCheckManager.CurrentPlayers += 1;
            m_LocalServerValuesChanged = true;
        }

        public void RemovePlayer()
        {
            m_ServerCheckManager.CurrentPlayers -= 1;
            m_LocalServerValuesChanged = true;
        }

        public void SetMap(string newMap)
        {

            m_ServerCheckManager.Map = newMap;
            m_LocalServerValuesChanged = true;
        }

        public void SetMode(string mode)
        {

            m_ServerCheckManager.GameType = mode;
            m_LocalServerValuesChanged = true;
        }

        public void UpdateServerIfChanged()
        {
            if (m_LocalServerValuesChanged)
            {
                m_ServerCheckManager.UpdateServerCheck();
                m_LocalServerValuesChanged = false;
            }
        }

        async Task ServerCheckLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UpdateServerIfChanged();
                await Task.Delay(1000);
            }
        }

        async Task<string> AwaitAllocationID()
        {
            var config = m_MultiplayService.ServerConfig;
            Debug.Log($"Awaiting Allocation. Server Config is:\n" +
                $"-ServerID: {config.ServerId}\n" +
                $"-AllocationID: {config.AllocatedUuid}\n" +
                $"-Port: {config.Port}\n" +
                $"-QPort: {config.QueryPort}\n" +
                $"-logs: {config.ServerLogDirectory}");

            //Waiting on OnMultiplayAllocation() event (Probably wont ever happen in a matchmaker scenario)
            while (string.IsNullOrEmpty(m_AllocationId))
            {
                var configID = config.AllocatedUuid;

                if (!string.IsNullOrEmpty(configID) && string.IsNullOrEmpty(m_AllocationId))
                {
                    Debug.Log($"Config had AllocationID: {configID}");
                    m_AllocationId = configID;
                }

                await Task.Delay(100);
            }

            return m_AllocationId;
        }

        /// <summary>
        /// This should be in the SDK but we can use web-requests to get access to the MatchmakerAllocationPayload
        /// </summary>
        /// <param name="allocationID"></param>
        /// <returns></returns>
        async Task<MatchmakerAllocationPayload> GetMatchmakerAllocationPayloadAsync(string allocationID)
        {
            Debug.Log($"Getting Allocation Payload with ID: {allocationID}");
            var payloadUrl = k_PayloadProxyUrl + $"/payload/{allocationID}";
            using var webRequest = UnityWebRequest.Get(payloadUrl);
            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Delay(50);
            }

            Debug.Log($"Web Request Text:{operation.webRequest.downloadHandler.text}");

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                    Debug.LogError(nameof(GetMatchmakerAllocationPayloadAsync) + ": ConnectionError: " +
                        webRequest.error);
                    break;
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(nameof(GetMatchmakerAllocationPayloadAsync) + ": Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(nameof(GetMatchmakerAllocationPayloadAsync) + ": HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log(nameof(GetMatchmakerAllocationPayloadAsync) + ":\nReceived: " +
                        webRequest.downloadHandler.text);
                    break;
                case UnityWebRequest.Result.InProgress:
                    break;
            }

            try
            {
                return JsonConvert.DeserializeObject<MatchmakerAllocationPayload>(webRequest.downloadHandler.text);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Something went wrong deserializing the Allocation Payload:\n{exception}");
                return null;
            }
        }

        void OnMultiplayAllocation(MultiplayAllocation allocation)
        {
            Debug.Log($"OnAllocation: {allocation.AllocationId}");
            if (string.IsNullOrEmpty(allocation.AllocationId))
                return;
            m_AllocationId = allocation.AllocationId;
        }

        void OnMultiplayDeAllocation(MultiplayDeallocation deallocation)
        {
            Debug.Log(
                $"Multiplay Deallocated : ID: {deallocation.AllocationId}\nEvent: {deallocation.EventId}\nServer{deallocation.ServerId}");
        }

        void OnMultiplayError(MultiplayError error)
        {
            Debug.Log($"MultiplayError : {error.Reason}\n{error.Detail}");
        }

        public void Dispose()
        {
            if (m_Servercallbacks != null)
            {
                m_Servercallbacks.Allocate -= OnMultiplayAllocation;
                m_Servercallbacks.Deallocate -= OnMultiplayDeAllocation;
                m_Servercallbacks.Error -= OnMultiplayError;
            }

            if (m_ServerCheckCancel != null)
                m_ServerCheckCancel.Cancel();

            m_ServerEvents?.UnsubscribeAsync();
        }
    }

    [Serializable]
    public class MatchmakerAllocationPayload
    {
        public MatchProperties MatchProperties;
        public string QueueName;
        public string PoolName;
        public string BackfillTicketId;

        public override string ToString()
        {
            StringBuilder payloadDescription = new StringBuilder();
            payloadDescription.AppendLine("Matchmaker Allocation Payload:");
            payloadDescription.AppendFormat("-QueueName: {0}\n", QueueName);
            payloadDescription.AppendFormat("-PoolName: {0}\n", PoolName);
            payloadDescription.AppendFormat("-ID: {0}\n", BackfillTicketId);
            payloadDescription.AppendFormat("-Teams: {0}\n", MatchProperties.Teams.Count);
            payloadDescription.AppendFormat("-Players: {0}\n", MatchProperties.Players.Count);
            payloadDescription.AppendFormat("-Region: {0}\n", MatchProperties.Region);
            return payloadDescription.ToString();
        }
    }
}