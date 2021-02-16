﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EchoClient.cs">
//   Copyright (c) 2020 Johannes Deml. All rights reserved.
// </copyright>
// <author>
//   Johannes Deml
//   public@deml.io
// </author>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;

namespace NetworkBenchmark.LiteNetLib
{
	internal class EchoClient : AClient, IClient
	{
		public override bool IsConnected => isConnected;
		public override bool IsDisposed => isDisposed;

		private bool isConnected;
		private bool isDisposed;
		private readonly int id;
		private readonly BenchmarkSetup config;
		private readonly BenchmarkData benchmarkData;

		private readonly byte[] message;
		private readonly EventBasedNetListener listener;
		private readonly NetManager netManager;
		private readonly DeliveryMethod deliveryMethod;
		private NetPeer peer;

		public EchoClient(int id, BenchmarkSetup config, BenchmarkData benchmarkData)
		{
			this.id = id;
			this.config = config;
			this.benchmarkData = benchmarkData;
			message = config.Message;

			switch (config.Transmission)
			{
				case TransmissionType.Reliable:
					deliveryMethod = DeliveryMethod.ReliableUnordered;
					break;
				case TransmissionType.Unreliable:
					deliveryMethod = DeliveryMethod.ReliableUnordered;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(config), $"Transmission Type {config.Transmission} not supported");
			}

			listener = new EventBasedNetListener();
			netManager = new NetManager(listener);
			if (!config.Address.Contains(':'))
			{
				netManager.IPv6Enabled = IPv6Mode.Disabled;
			}

			netManager.UpdateTime = Utilities.CalculateTimeout(config.ClientTickRate);
			netManager.UnsyncedEvents = true;
			netManager.DisconnectTimeout = 10000;

			isConnected = false;
			isDisposed = false;

			listener.PeerConnectedEvent += OnPeerConnected;
			listener.PeerDisconnectedEvent += OnPeerDisconnected;
			listener.NetworkReceiveEvent += OnNetworkReceive;
			listener.NetworkErrorEvent += OnNetworkError;
		}

		public override void StartClient()
		{
			base.StartClient();
			netManager.Start();
			peer = netManager.Connect(config.Address, config.Port, "ConnectionKey");
			isDisposed = false;
		}

		public override void StartBenchmark()
		{
			base.StartBenchmark();
			var parallelMessagesPerClient = config.ParallelMessages;

			for (int i = 0; i < parallelMessagesPerClient; i++)
			{
				Send(message);
			}

			netManager.TriggerUpdate();
		}

		public override void DisconnectClient()
		{
			if (!IsConnected)
			{
				return;
			}

			var clientDisconnected = Task.Factory.StartNew(() => { peer.Disconnect(); }, TaskCreationOptions.LongRunning);
		}

		public Task StopClient()
		{
			base.StopClient();
			var stopClient = Task.Factory.StartNew(() => { netManager.Stop(false); }, TaskCreationOptions.LongRunning);

			return stopClient;
		}

		public override void Dispose()
		{
			listener.PeerConnectedEvent -= OnPeerConnected;
			listener.PeerDisconnectedEvent -= OnPeerDisconnected;
			listener.NetworkReceiveEvent -= OnNetworkReceive;
			listener.NetworkErrorEvent -= OnNetworkError;

			isDisposed = true;
		}

		private void Send(byte[] bytes)
		{
			if (!IsConnected)
			{
				return;
			}

			peer.Send(bytes, deliveryMethod);
			Interlocked.Increment(ref benchmarkData.MessagesClientSent);
		}

		private void OnPeerConnected(NetPeer peer)
		{
			isConnected = true;
		}

		private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
		{
			if (disconnectInfo.Reason == DisconnectReason.Timeout && benchmarkRunning)
			{
				Utilities.WriteVerboseLine($"Client {id} disconnected due to timeout. Probably the server is overwhelmed by the requests.");
				Interlocked.Increment(ref benchmarkData.Errors);
			}

			this.peer = null;
			isConnected = false;
		}

		private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliverymethod)
		{
			if (benchmarkRunning)
			{
				Interlocked.Increment(ref benchmarkData.MessagesClientReceived);
				Send(message);
				netManager.TriggerUpdate();
			}

			reader.Recycle();
		}

		private void OnNetworkError(IPEndPoint endpoint, SocketError socketerror)
		{
			if (benchmarkRunning)
			{
				Utilities.WriteVerboseLine($"Error Client {id}: {socketerror}");
				Interlocked.Increment(ref benchmarkData.Errors);
			}
		}
	}
}
