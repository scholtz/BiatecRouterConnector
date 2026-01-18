using System;
using System.Collections.Generic;
using System.Text;

namespace BiatecRouterConnector
{
    public static class Extensions
    {
        public static BiatecRouterConnector.Generated.TransactionParametersResponse ToRouterParams(this Algorand.Algod.Model.TransactionParametersResponse txParams)
        {
            if (txParams is null) throw new ArgumentNullException(nameof(txParams));
            return new Generated.TransactionParametersResponse
            {
                ConsensusVersion = txParams.ConsensusVersion,
                Fee = Convert.ToInt64(txParams.Fee),
                GenesisHash = txParams.GenesisHash,
                GenesisId = txParams.GenesisId,
                LastRound = Convert.ToInt64(txParams.LastRound),
                MinFee = Convert.ToInt64(txParams.MinFee),
            };
        }
    }
}
