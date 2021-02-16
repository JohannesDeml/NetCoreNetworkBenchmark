﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EchoClient.cs">
//   Copyright (c) 2021 Johannes Deml. All rights reserved.
// </copyright>
// <author>
//   Johannes Deml
//   public@deml.io
// </author>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using kcp2k;

namespace NetworkBenchmark.Kcp2k
{
	internal class EchoClient
	{
		public bool IsConnected { get; private set; }
		public bool IsDisposed { get; private set; }

		private readonly int id;
		private readonly BenchmarkSetup config;
		private readonly BenchmarkData benchmarkData;

		private readonly Thread tickThread;
		private readonly byte[] messageArray;
		private readonly KcpClientConnection client;
		private KcpChannel communicationChannel;
		private bool noDelay;

		public EchoClient(int id, BenchmarkSetup config, BenchmarkData benchmarkData)
		{
			this.id = id;
			this.config = config;
			this.benchmarkData = benchmarkData;
			messageArray = config.Message;
			noDelay = true;

			switch (config.TransmissionType)
			{
				case TransmissionType.Reliable:
					communicationChannel = KcpChannel.Reliable;
					break;
				case TransmissionType.Unreliable:
					communicationChannel = KcpChannel.Unreliable;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(config), $"Transmission Type {config.TransmissionType} not supported");
			}

			client = new KcpClientConnection();

			client.OnAuthenticated = OnPeerConnected;
			client.OnData = OnNetworkReceive;
			client.OnDisconnected = OnPeerDisconnected;

			tickThread = new Thread(TickLoop);
			tickThread.Name = $"Kcp2k Client {id}";
			tickThread.IsBackground = true;

			IsConnected = false;
			IsDisposed = false;
		}

		public void Start()
		{
			var interval = (uint) Utilities.CalculateTimeout(config.ClientTickRate);
			Connect(config.Address, (ushort) config.Port, noDelay, interval);
			tickThread.Start();
			IsDisposed = false;
		}

		private void Connect(string address, ushort port, bool useNoDelay, uint interval, int fastResend = 0, bool congestionWindow = true, uint sendWindowSize = Kcp.WND_SND, uint receiveWindowSize = Kcp.WND_RCV)
		{
			client.Connect(address, port, useNoDelay, interval, fastResend, congestionWindow, sendWindowSize, receiveWindowSize);
		}

		private void TickLoop()
		{
			while (benchmarkData.Listen)
			{
				Tick();
				Thread.Sleep(1);
			}
		}

		private void Tick()
		{
			client.RawReceive();
			client.Tick();
		}

		public void StartSendingMessages()
		{
			var parallelMessagesPerClient = config.ParallelMessages;

			for (int i = 0; i < parallelMessagesPerClient; i++)
			{
				Send(messageArray, communicationChannel);
			}

			Tick();
		}

		public Task Disconnect()
		{
			if (!IsConnected)
			{
				return Task.CompletedTask;
			}

			var clientDisconnected = Task.Factory.StartNew(() => { client.Disconnect(); }, TaskCreationOptions.LongRunning);

			return clientDisconnected;
		}

		public async void Dispose()
		{
			while (tickThread.IsAlive)
			{
				await Task.Delay(10);
			}

			//TODO client.Dispose();
			IsDisposed = true;
		}

		private void Send(ArraySegment<byte> message, KcpChannel channel)
		{
			if (!IsConnected)
			{
				return;
			}

			client.SendData(message, channel);
			Interlocked.Increment(ref benchmarkData.MessagesClientSent);
		}

		private void OnPeerConnected()
		{
			Console.WriteLine("Benchmark client connected");
			IsConnected = true;
		}

		private void OnNetworkReceive(ArraySegment<byte> arraySegment)
		{
			if (benchmarkData.Running)
			{
				Interlocked.Increment(ref benchmarkData.MessagesClientReceived);
				Send(messageArray, communicationChannel);
			}
		}

		private void OnPeerDisconnected()
		{
			IsConnected = false;
		}
	}
}
