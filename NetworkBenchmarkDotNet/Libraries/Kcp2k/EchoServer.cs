﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EchoServer.cs">
//   Copyright (c) 2021 Johannes Deml. All rights reserved.
// </copyright>
// <author>
//   Johannes Deml
//   public@deml.io
// </author>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using kcp2k;

namespace NetworkBenchmark.Kcp2k
{
	internal class EchoServer : IServer
	{
		public bool IsStarted => serverThread != null && serverThread.IsAlive && server.IsActive();

		private readonly BenchmarkSetup config;
		private readonly BenchmarkData benchmarkData;
		private readonly KcpServer server;
		private readonly Thread serverThread;
		private readonly KcpChannel communicationChannel;
		private readonly bool noDelay;

		private readonly byte[] message;

		public EchoServer(BenchmarkSetup config, BenchmarkData benchmarkData)
		{
			this.config = config;
			this.benchmarkData = benchmarkData;
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


			var interval = (uint) Utilities.CalculateTimeout(config.ServerTickRate);
			server = new KcpServer(OnConnected, OnReceiveMessage, OnDisconnected, noDelay, interval);


			message = new byte[config.MessageByteSize];

			serverThread = new Thread(TickLoop);
			serverThread.Name = "Kcp2k Server";
			serverThread.Priority = ThreadPriority.AboveNormal;
		}

		public void StartServerThread()
		{
			serverThread.Start();
		}

		private void TickLoop()
		{
			server.Start((ushort) config.Port);

			while (benchmarkData.Listen)
			{
				server.Tick();
				TimeUtilities.HighPrecisionThreadSleep(1);
			}

			server.Stop();
		}

		private void OnConnected(int connectionId)
		{
			if (benchmarkData.Running)
			{
				Utilities.WriteVerboseLine($"Client {connectionId} connected while benchmark is running.");
			}
		}

		private void OnReceiveMessage(int connectionId, ArraySegment<byte> arraySegment)
		{
			if (benchmarkData.Running)
			{
				Interlocked.Increment(ref benchmarkData.MessagesServerReceived);
				Array.Copy(arraySegment.Array, arraySegment.Offset, message, 0, arraySegment.Count);
				Send(connectionId, message, communicationChannel);
			}
		}

		private void OnDisconnected(int connectionId)
		{
			if (benchmarkData.Preparing || benchmarkData.Running)
			{
				Utilities.WriteVerboseLine($"Client {connectionId} disconnected while benchmark is running.");
			}
		}

		private void Send(int connectionId, ArraySegment<byte> message, KcpChannel channel)
		{
			server.Send(connectionId, message, channel);
			Interlocked.Increment(ref benchmarkData.MessagesServerSent);
		}

		public void Dispose()
		{
			// TODO server.Dispose();
		}
	}
}
