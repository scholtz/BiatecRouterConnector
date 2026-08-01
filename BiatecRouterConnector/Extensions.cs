namespace BiatecRouterConnector
{
    /// <summary>Conversion helpers between Algorand SDK types and the generated router API types.</summary>
    public static class Extensions
    {
        /// <summary>
        /// Maps an Algod <see cref="Algorand.Algod.Model.TransactionParametersResponse"/> to the router
        /// API's <see cref="BiatecRouterConnector.Generated.TransactionParametersResponse"/>, for use as
        /// <c>RouteInputParameters.TransParams</c>.
        /// </summary>
        public static BiatecRouterConnector.Generated.TransactionParametersResponse ToRouterParams(this Algorand.Algod.Model.TransactionParametersResponse txParams)
        {
            ArgumentNullException.ThrowIfNull(txParams);
            return new Generated.TransactionParametersResponse
            {
                ConsensusVersion = txParams.ConsensusVersion,
                Fee = Convert.ToUInt64(txParams.Fee),
                GenesisHash = txParams.GenesisHash,
                GenesisId = txParams.GenesisId,
                LastRound = Convert.ToUInt64(txParams.LastRound),
                MinFee = Convert.ToUInt64(txParams.MinFee),
            };
        }
    }
}
