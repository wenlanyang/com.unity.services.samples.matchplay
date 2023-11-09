using System;
using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine.TestTools;
using Matchplay.Client;
using Matchplay.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Matchplay.Tests
{
    public class StressTests
    {
        NetworkManager m_TestManager;

        [OneTimeSetUp]
        [RequiresPlayMode]
        public void OneTimeSetup()
        {
            m_TestManager = TestResources.TestNetworkManager();
            SceneManager.LoadScene("bootStrap");
        }

        [TearDown]
        [RequiresPlayMode]
        public void TearDown()
        {
            AuthenticationWrapper.SignOut();
            if (m_TestManager.IsListening)
            {
                m_TestManager.Shutdown();
            }
        }

        [UnityTest]
        [RequiresPlayMode]
        public IEnumerator Client_Play_Forever_On_Server()
        {
            ClientGameManager gameManager = ClientSingleton.Instance.Manager;
            gameManager.SetGameMap(Map.Space);
            gameManager.SetGameMode(GameMode.Meditating);
            gameManager.SetGameQueue(GameQueue.Casual);
            AwaitAuthenticationOrTimeout();

            string ip = Environment.GetEnvironmentVariable("SERVER_IP");
            if (ip == null)
            {
                Debug.Log("Environment variable SERVER_IP not set");
                yield return null;
            }

            gameManager.BeginConnection(ip, 9000);
            yield return new WaitUntil(() => false);
        }

        async void AwaitAuthenticationOrTimeout()
        {
            AuthState authState = await AuthenticationWrapper.Authenticating();
            if (authState == AuthState.Authenticated)
            {
                Debug.Log("Client authenticated");
            }
            else
            {
                Debug.Log("Client failed to authenticate");
            }
        }
    }
}