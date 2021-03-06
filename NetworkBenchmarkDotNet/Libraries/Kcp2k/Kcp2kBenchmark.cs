﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Kcp2kBenchmark.cs">
//   Copyright (c) 2021 Johannes Deml. All rights reserved.
// </copyright>
// <author>
//   Johannes Deml
//   public@deml.io
// </author>
// --------------------------------------------------------------------------------------------------------------------

using System;
using kcp2k;

namespace NetworkBenchmark.Kcp2k
{
	internal class Kcp2kBenchmark : ANetworkBenchmark
	{
		protected override IServer CreateNewServer(Configuration config, BenchmarkStatistics statistics)
		{
			return new EchoServer(config, statistics);
		}

		protected override IClient CreateNewClient(int id, Configuration config, BenchmarkStatistics statistics)
		{
			return new EchoClient(id, config, statistics);
		}

		public static KcpChannel GetChannel(TransmissionType transmissionType)
		{
			switch (transmissionType)
			{
				case TransmissionType.Reliable:
					return KcpChannel.Reliable;
				case TransmissionType.Unreliable:
					return KcpChannel.Unreliable;
				default:
					throw new ArgumentOutOfRangeException(nameof(transmissionType), $"Transmission Type {transmissionType} not supported");
			}
		}
	}
}
