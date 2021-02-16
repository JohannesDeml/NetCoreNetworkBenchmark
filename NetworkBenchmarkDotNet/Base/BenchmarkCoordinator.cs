﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BenchmarkCoordinator.cs">
//   Copyright (c) 2020 Johannes Deml. All rights reserved.
// </copyright>
// <author>
//   Johannes Deml
//   public@deml.io
// </author>
// --------------------------------------------------------------------------------------------------------------------

using System.Text;
using System.Threading;

namespace NetworkBenchmark
{
	public static class BenchmarkCoordinator
	{
		public static BenchmarkSetup Config { get; set; }
		public static readonly BenchmarkStatistics BenchmarkStatistics = new BenchmarkStatistics();

		public static void ApplyPredefinedConfiguration()
		{
			if (Config == null)
			{
				Config = new BenchmarkSetup();
			}

			BenchmarkSetup.ApplyPredefinedBenchmarkConfiguration(Config);
		}

		public static void PrepareBenchmark(INetworkBenchmark networkBenchmark)
		{
			Utilities.WriteVerbose("-> Prepare Benchmark.");
			Config.PrepareForNewBenchmark();
			networkBenchmark.Initialize(Config, BenchmarkStatistics);
			Utilities.WriteVerbose(".");

			var serverTask = networkBenchmark.StartServer();
			var clientTask = networkBenchmark.StartClients();
			serverTask.Wait();
			clientTask.Wait();
			Utilities.WriteVerbose(".");

			networkBenchmark.ConnectClients().Wait();
			Utilities.WriteVerboseLine(" Done");
		}

		public static void RunTimedBenchmark(INetworkBenchmark networkBenchmark)
		{
			Utilities.WriteVerbose($"-> Run Benchmark {Config.Library}...");
			BenchmarkCoordinator.StartBenchmark(networkBenchmark);

			Thread.Sleep(Config.Duration * 1000);

			BenchmarkCoordinator.StopBenchmark(networkBenchmark);
			Utilities.WriteVerboseLine(" Done");
		}

		public static void StartBenchmark(INetworkBenchmark networkBenchmark)
		{
			BenchmarkStatistics.Reset();
			BenchmarkStatistics.StartBenchmark();
			networkBenchmark.StartBenchmark();
		}

		public static void StopBenchmark(INetworkBenchmark networkBenchmark)
		{
			networkBenchmark.StopBenchmark();
			BenchmarkStatistics.StopBenchmark();
		}

		public static void CleanupBenchmark(INetworkBenchmark networkBenchmark)
		{
			Utilities.WriteVerbose("-> Clean up.");
			networkBenchmark.DisconnectClients().Wait();

			networkBenchmark.StopClients().Wait();
			networkBenchmark.DisposeClients().Wait();
			Utilities.WriteVerbose(".");


			networkBenchmark.StopServer().Wait();
			Utilities.WriteVerbose(".");
			networkBenchmark.DisposeServer().Wait();
			networkBenchmark.Deinitialize();
			Utilities.WriteVerboseLine(" Done");
			Utilities.WriteVerboseLine("");
		}

		public static string PrintStatistics()
		{
			var sb = new StringBuilder();

			sb.AppendLine("```");
			sb.AppendLine($"Results {Config.Library} with {Config.Transmission} {Config.Test}");
			if (BenchmarkStatistics.Errors > 0)
			{
				sb.AppendLine($"Errors: {BenchmarkStatistics.Errors}");
				sb.AppendLine();
			}

			sb.AppendLine($"Duration: {BenchmarkStatistics.Duration.TotalSeconds:0.000} s");
			sb.AppendLine($"Messages sent by clients: {BenchmarkStatistics.MessagesClientSent:n0}");
			sb.AppendLine($"Messages server received: {BenchmarkStatistics.MessagesServerReceived:n0}");
			sb.AppendLine($"Messages sent by server: {BenchmarkStatistics.MessagesServerSent:n0}");
			sb.AppendLine($"Messages clients received: {BenchmarkStatistics.MessagesClientReceived:n0}");
			sb.AppendLine();

			var totalBytes = BenchmarkStatistics.MessagesClientReceived * Config.MessageByteSize;
			var totalMb = totalBytes / (1024.0d * 1024.0d);
			var latency = BenchmarkStatistics.Duration.TotalMilliseconds / (BenchmarkStatistics.MessagesClientReceived / 1000.0d);

			sb.AppendLine($"Total data: {totalMb:0.00} MB");
			sb.AppendLine($"Data throughput: {totalMb / BenchmarkStatistics.Duration.TotalSeconds:0.00} MB/s");
			sb.AppendLine($"Message throughput: {BenchmarkStatistics.MessagesClientReceived / BenchmarkStatistics.Duration.TotalSeconds:n0} msg/s");
			sb.AppendLine($"Message latency: {latency:0.000} μs");
			sb.AppendLine("```");
			sb.AppendLine();

			return sb.ToString();
		}
	}
}
