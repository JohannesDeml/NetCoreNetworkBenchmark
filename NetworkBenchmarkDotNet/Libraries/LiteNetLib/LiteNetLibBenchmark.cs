﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LiteNetLibBenchmark.cs">
//   Copyright (c) 2020 Johannes Deml. All rights reserved.
// </copyright>
// <author>
//   Johannes Deml
//   public@deml.io
// </author>
// --------------------------------------------------------------------------------------------------------------------
namespace NetworkBenchmark.LiteNetLib
{
	internal class LiteNetLibBenchmark : ANetworkBenchmark
	{
		protected override IServer CreateNewServer(BenchmarkSetup setup, BenchmarkData data)
		{
			return new EchoServer(setup, data);
		}

		protected override IClient CreateNewClient(int id, BenchmarkSetup setup, BenchmarkData data)
		{
			return new EchoClient(id, setup, data);
		}
	}
}
