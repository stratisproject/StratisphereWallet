﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace Stratis.Bitcoin.Networks
{
    public sealed class StraxTest : Network
    {
        public StraxTest()
        {
            this.Name = "StraxTest";
            this.NetworkType = NetworkType.Testnet;
            this.Magic = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("TtrX"), 0);
            this.DefaultPort = 27105;
            this.DefaultMaxOutboundConnections = 16;
            this.DefaultMaxInboundConnections = 109;
            this.DefaultRPCPort = 27104;
            this.DefaultAPIPort = 27103;
            this.DefaultSignalRPort = 27102;
            this.MaxTipAge = 2 * 60 * 60;
            this.MinTxFee = 10000;
            this.FallbackFee = 10000;
            this.MinRelayTxFee = 10000;
            this.RootFolderName = StraxNetwork.StraxRootFolderName;
            this.DefaultConfigFilename = StraxNetwork.StraxDefaultConfigFilename;
            this.MaxTimeOffsetSeconds = 25 * 60;
            this.CoinTicker = "TSTRAX";
            this.DefaultBanTimeSeconds = 11250; // 500 (MaxReorg) * 45 (TargetSpacing) / 2 = 3 hours, 7 minutes and 30 seconds

            this.CirrusRewardDummyAddress = "tGXZrZiU44fx3SQj8tAQ3Zexy2VuELZtoh";
            this.RewardClaimerBatchActivationHeight = 166200;
            this.RewardClaimerBlockInterval = 100;

            var consensusFactory = new PosConsensusFactory();

            // Create the genesis block.
            this.GenesisTime = 1598918400; // 1 September 2020
            this.GenesisNonce = 109534; // TODO: Update this once the final block is mined
            this.GenesisBits = new Target(new uint256("0000ffff00000000000000000000000000000000000000000000000000000000")).ToCompact(); // This should be set to the same as the PowLimit
            this.GenesisVersion = 1;
            this.GenesisReward = Money.Zero;

            Block genesisBlock = StraxNetwork.CreateGenesisBlock(consensusFactory, this.GenesisTime, this.GenesisNonce, this.GenesisBits, this.GenesisVersion, this.GenesisReward, "teststraxgenesisblock");

            this.Genesis = genesisBlock;

            // Taken from Stratis.
            var consensusOptions = new PosConsensusOptions(
                maxBlockBaseSize: 1_000_000,
                maxStandardVersion: 2,
                maxStandardTxWeight: 150_000,
                maxBlockSigopsCost: 20_000,
                maxStandardTxSigopsCost: 20_000 / 2,
                witnessScaleFactor: 4
                );

            var buriedDeployments = new BuriedDeploymentsArray
            {
                [BuriedDeployments.BIP34] = 0,
                [BuriedDeployments.BIP65] = 0,
                [BuriedDeployments.BIP66] = 0
            };

            // To successfully process the OP_FEDERATION opcode the federations should be known.
            this.Federations = new Federations();

            // This should mirror the federation registered in CirrusTest.
            this.Federations.RegisterFederation(new Federation(new[] {
               new PubKey("021040ef28c82fcffb63028e69081605ed4712910c8384d5115c9ffeacd9dbcae4"),//Node1
               new PubKey("0244290a31824ba7d53e59c7a29d13dbeca15a9b0d36fdd4d28fce426753107bfc"),//Node2
               new PubKey("032df4a2d62c0db12cd1d66201819a10788637c9b90a1cd2a5a3f5196fdab7a621"),//Node3
               new PubKey("028ed190eb4ed6e46440ac6af21d8a67a537bd1bd7edb9cc5177d36d5a0972244d"),//Node4
               new PubKey("02ff9923324399a188daf4310825a85dd3b89e2301d0ad073295b6f33ae1c72f7a"),//Node5
               new PubKey("030e03b808ddb51701d4d3dbc0a74a6f9aedfecf23d5f874914641fc81197b239a"),//Node7
               new PubKey("02270d6c20d3393fad7f74c59d2d26b0824ed016ccbc15e698e7354314459a60a5"),//Node8
            }));

            this.Consensus = new NBitcoin.Consensus(
                consensusFactory: consensusFactory,
                consensusOptions: consensusOptions,
                coinType: 1, // Per https://github.com/satoshilabs/slips/blob/master/slip-0044.md - testnets share a cointype
                hashGenesisBlock: genesisBlock.GetHash(),
                subsidyHalvingInterval: 210000,
                majorityEnforceBlockUpgrade: 750,
                majorityRejectBlockOutdated: 950,
                majorityWindow: 1000,
                buriedDeployments: buriedDeployments,
                bip9Deployments: null,
                bip34Hash: null,
                minerConfirmationWindow: 2016,
                maxReorgLength: 500,
                defaultAssumeValid: null,
                maxMoney: long.MaxValue,
                coinbaseMaturity: 50,
                premineHeight: 2,
                premineReward: Money.Coins(130000000),
                proofOfWorkReward: Money.Coins(18),
                powTargetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60),
                targetSpacing: TimeSpan.FromSeconds(45),
                powAllowMinDifficultyBlocks: false,
                posNoRetargeting: false,
                powNoRetargeting: false,
                powLimit: new Target(new uint256("0000ffff00000000000000000000000000000000000000000000000000000000")),
                minimumChainWork: null,
                isProofOfStake: true,
                lastPowBlock: 12500,
                proofOfStakeLimit: new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeLimitV2: new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeReward: Money.Coins(18)
            );

            this.Consensus.PosEmptyCoinbase = false;

            this.Base58Prefixes = new byte[12][];
            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { 120 }; // q
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { 127 }; // t
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (120 + 128) };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            this.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            this.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
            this.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            this.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            this.Base58Prefixes[(int)Base58Type.STEALTH_ADDRESS] = new byte[] { 0x2a };
            this.Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };
            this.Base58Prefixes[(int)Base58Type.COLORED_ADDRESS] = new byte[] { 0x13 };
            
            this.Bech32Encoders = new Bech32Encoder[2];
            var encoder = new Bech32Encoder("tstrax");
            this.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            this.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            this.DNSSeeds = new List<DNSSeedData>
            {
                new DNSSeedData("testnet1.stratisnetwork.com", "testnet1.stratisnetwork.com")
            };

            this.SeedNodes = new List<NetworkAddress>
            {
                new NetworkAddress(IPAddress.Parse("82.146.153.140"), 27105), // Iain
            };
            this.StandardScriptsRegistry = new StraxStandardScriptsRegistry();

            Assert(this.DefaultBanTimeSeconds <= this.Consensus.MaxReorgLength * this.Consensus.TargetSpacing.TotalSeconds / 2);
            
            Assert(this.Consensus.HashGenesisBlock == uint256.Parse("0x0000db68ff9e74fbaf7654bab4fa702c237318428fa9186055c243ddde6354ca"));
            Assert(this.Genesis.Header.HashMerkleRoot == uint256.Parse("0xfe6317d42149b091399e7f834ca32fd248f8f26f493c30a35d6eea692fe4fcad"));
        }
    }
}
