using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Consensus.Core.Services;

/// <summary>
/// Interface for transaction pool management
/// </summary>
public interface ITransactionPoolService
{
    /// <summary>
    /// Adds a transaction to the pool
    /// </summary>
    Task<bool> AddTransactionAsync(Transaction transaction);
    
    /// <summary>
    /// Removes a transaction from the pool
    /// </summary>
    Task<bool> RemoveTransactionAsync(Guid transactionId);
    
    /// <summary>
    /// Gets pending transactions for block creation
    /// </summary>
    Task<IList<Transaction>> GetPendingTransactionsAsync(TransactionPoolRequest request);
    
    /// <summary>
    /// Clears confirmed transactions from the pool
    /// </summary>
    Task ClearConfirmedTransactionsAsync(IEnumerable<Transaction> confirmedTransactions);
    
    /// <summary>
    /// Gets transaction pool statistics
    /// </summary>
    Task<TransactionPoolStats> GetPoolStatsAsync();
    
    /// <summary>
    /// Validates and cleans up the transaction pool
    /// </summary>
    Task CleanupPoolAsync();
}

/// <summary>
/// Transaction pool service implementation
/// </summary>
public class TransactionPoolService : ITransactionPoolService
{
    private readonly IBlockValidator _validator;
    private readonly ILogger<TransactionPoolService> _logger;
    private readonly ConcurrentDictionary<Guid, Transaction> _transactionPool;
    private readonly ConcurrentDictionary<string, List<Guid>> _accountNonces; // Track nonces per account
    private readonly object _poolLock = new();

    public TransactionPoolService(IBlockValidator validator, ILogger<TransactionPoolService> logger)
    {
        _validator = validator;
        _logger = logger;
        _transactionPool = new ConcurrentDictionary<Guid, Transaction>();
        _accountNonces = new ConcurrentDictionary<string, List<Guid>>();
    }

    public async Task<bool> AddTransactionAsync(Transaction transaction)
    {
        _logger.LogDebug("Adding transaction {TransactionId} to pool", transaction.Id);

        try
        {
            // Validate transaction before adding to pool
            var existingTransactions = _transactionPool.Values.Where(t => t.FromAddress == transaction.FromAddress);
            var validation = await _validator.ValidateTransactionAsync(transaction, existingTransactions);
            
            if (!validation.IsValid)
            {
                _logger.LogWarning("Rejecting invalid transaction {TransactionId}: {Error}", 
                    transaction.Id, validation.ErrorMessage);
                return false;
            }

            // Check for duplicate transactions
            if (_transactionPool.ContainsKey(transaction.Id))
            {
                _logger.LogDebug("Transaction {TransactionId} already in pool", transaction.Id);
                return false;
            }

            // Check for transaction with same hash
            if (_transactionPool.Values.Any(t => t.Hash == transaction.Hash))
            {
                _logger.LogWarning("Transaction with hash {Hash} already exists in pool", transaction.Hash);
                return false;
            }

            // Add to pool
            lock (_poolLock)
            {
                _transactionPool[transaction.Id] = transaction;
                
                // Track nonce for account
                if (!string.IsNullOrEmpty(transaction.FromAddress))
                {
                    if (!_accountNonces.ContainsKey(transaction.FromAddress))
                    {
                        _accountNonces[transaction.FromAddress] = new List<Guid>();
                    }
                    _accountNonces[transaction.FromAddress].Add(transaction.Id);
                }
            }

            _logger.LogDebug("Successfully added transaction {TransactionId} to pool", transaction.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding transaction {TransactionId} to pool", transaction.Id);
            return false;
        }
    }

    public async Task<bool> RemoveTransactionAsync(Guid transactionId)
    {
        _logger.LogDebug("Removing transaction {TransactionId} from pool", transactionId);

        try
        {
            lock (_poolLock)
            {
                if (_transactionPool.TryRemove(transactionId, out var transaction))
                {
                    // Remove from account nonce tracking
                    if (!string.IsNullOrEmpty(transaction.FromAddress) && 
                        _accountNonces.TryGetValue(transaction.FromAddress, out var nonces))
                    {
                        nonces.Remove(transactionId);
                        if (!nonces.Any())
                        {
                            _accountNonces.TryRemove(transaction.FromAddress, out _);
                        }
                    }

                    _logger.LogDebug("Successfully removed transaction {TransactionId} from pool", transactionId);
                    return true;
                }
            }

            _logger.LogDebug("Transaction {TransactionId} not found in pool", transactionId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing transaction {TransactionId} from pool", transactionId);
            return false;
        }
    }

    public async Task<IList<Transaction>> GetPendingTransactionsAsync(TransactionPoolRequest request)
    {
        _logger.LogDebug("Getting pending transactions from pool");

        try
        {
            var transactions = _transactionPool.Values
                .Where(t => t.Status == TransactionStatus.Pending)
                .Where(t => request.SimulationId == null || t.SimulationRunId == request.SimulationId)
                .ToList();

            // Apply sorting based on request
            transactions = request.SortBy switch
            {
                TransactionSortBy.Fee => transactions.OrderByDescending(t => t.Fee).ToList(),
                TransactionSortBy.GasPrice => transactions.OrderByDescending(t => t.GasPrice).ToList(),
                TransactionSortBy.Timestamp => transactions.OrderBy(t => t.CreatedAt).ToList(),
                TransactionSortBy.Nonce => transactions.OrderBy(t => t.Nonce).ToList(),
                _ => transactions.OrderByDescending(t => t.Fee).ThenBy(t => t.CreatedAt).ToList()
            };

            // Apply limits
            if (request.MaxCount > 0)
            {
                transactions = transactions.Take(request.MaxCount).ToList();
            }

            // Filter by minimum fee if specified
            if (request.MinimumFee > 0)
            {
                transactions = transactions.Where(t => t.Fee >= request.MinimumFee).ToList();
            }

            _logger.LogDebug("Returning {Count} pending transactions", transactions.Count);
            return transactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending transactions from pool");
            return new List<Transaction>();
        }
    }

    public async Task ClearConfirmedTransactionsAsync(IEnumerable<Transaction> confirmedTransactions)
    {
        _logger.LogDebug("Clearing {Count} confirmed transactions from pool", confirmedTransactions.Count());

        try
        {
            var clearedCount = 0;
            
            foreach (var transaction in confirmedTransactions)
            {
                if (await RemoveTransactionAsync(transaction.Id))
                {
                    clearedCount++;
                }
            }

            _logger.LogInformation("Cleared {ClearedCount} confirmed transactions from pool", clearedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing confirmed transactions from pool");
        }
    }

    public async Task<TransactionPoolStats> GetPoolStatsAsync()
    {
        try
        {
            var transactions = _transactionPool.Values.ToList();
            
            var stats = new TransactionPoolStats
            {
                TotalTransactions = transactions.Count,
                PendingTransactions = transactions.Count(t => t.Status == TransactionStatus.Pending),
                ConfirmedTransactions = transactions.Count(t => t.Status == TransactionStatus.Confirmed),
                RejectedTransactions = transactions.Count(t => t.Status == TransactionStatus.Rejected),
                FailedTransactions = transactions.Count(t => t.Status == TransactionStatus.Failed),
                TotalFees = transactions.Sum(t => t.Fee),
                AverageFee = transactions.Any() ? transactions.Average(t => t.Fee) : 0,
                HighestFee = transactions.Any() ? transactions.Max(t => t.Fee) : 0,
                LowestFee = transactions.Any() ? transactions.Min(t => t.Fee) : 0,
                TotalGas = transactions.Sum(t => t.GasLimit),
                AverageGasPrice = transactions.Any() ? transactions.Average(t => t.GasPrice) : 0,
                UniqueAccounts = _accountNonces.Keys.Count,
                PoolSizeBytes = CalculatePoolSize(transactions),
                OldestTransaction = transactions.Any() ? transactions.Min(t => t.CreatedAt) : null,
                NewestTransaction = transactions.Any() ? transactions.Max(t => t.CreatedAt) : null
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating transaction pool stats");
            return new TransactionPoolStats();
        }
    }

    public async Task CleanupPoolAsync()
    {
        _logger.LogDebug("Starting transaction pool cleanup");

        try
        {
            var cleanupTasks = new List<Task>
            {
                RemoveExpiredTransactionsAsync(),
                RemoveInvalidTransactionsAsync(),
                RemoveStaleTransactionsAsync(),
                RemoveDuplicateTransactionsAsync()
            };

            await Task.WhenAll(cleanupTasks);

            _logger.LogInformation("Transaction pool cleanup completed. Pool size: {PoolSize}", _transactionPool.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during transaction pool cleanup");
        }
    }

    // Private helper methods
    private async Task RemoveExpiredTransactionsAsync()
    {
        var expiredCutoff = DateTime.UtcNow.AddHours(-24); // Remove transactions older than 24 hours
        var expiredTransactions = _transactionPool.Values
            .Where(t => t.CreatedAt < expiredCutoff)
            .ToList();

        foreach (var transaction in expiredTransactions)
        {
            await RemoveTransactionAsync(transaction.Id);
        }

        if (expiredTransactions.Any())
        {
            _logger.LogInformation("Removed {Count} expired transactions from pool", expiredTransactions.Count);
        }
    }

    private async Task RemoveInvalidTransactionsAsync()
    {
        var invalidTransactions = new List<Transaction>();

        foreach (var transaction in _transactionPool.Values)
        {
            var otherTransactions = _transactionPool.Values.Where(t => t.Id != transaction.Id);
            var validation = await _validator.ValidateTransactionAsync(transaction, otherTransactions);
            
            if (!validation.IsValid)
            {
                invalidTransactions.Add(transaction);
            }
        }

        foreach (var transaction in invalidTransactions)
        {
            await RemoveTransactionAsync(transaction.Id);
        }

        if (invalidTransactions.Any())
        {
            _logger.LogInformation("Removed {Count} invalid transactions from pool", invalidTransactions.Count);
        }
    }

    private async Task RemoveStaleTransactionsAsync()
    {
        var staleCutoff = DateTime.UtcNow.AddHours(-6); // Consider transactions stale after 6 hours
        var staleTransactions = _transactionPool.Values
            .Where(t => t.CreatedAt < staleCutoff && t.Status == TransactionStatus.Pending)
            .ToList();

        foreach (var transaction in staleTransactions)
        {
            transaction.Status = TransactionStatus.Rejected;
            _logger.LogDebug("Marked stale transaction {TransactionId} as rejected", transaction.Id);
        }

        if (staleTransactions.Any())
        {
            _logger.LogInformation("Marked {Count} stale transactions as rejected", staleTransactions.Count);
        }
    }

    private async Task RemoveDuplicateTransactionsAsync()
    {
        var duplicateGroups = _transactionPool.Values
            .GroupBy(t => t.Hash)
            .Where(g => g.Count() > 1)
            .ToList();

        var removedCount = 0;

        foreach (var group in duplicateGroups)
        {
            // Keep the oldest transaction, remove the rest
            var transactionsToRemove = group.OrderBy(t => t.CreatedAt).Skip(1);
            
            foreach (var transaction in transactionsToRemove)
            {
                await RemoveTransactionAsync(transaction.Id);
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            _logger.LogInformation("Removed {Count} duplicate transactions from pool", removedCount);
        }
    }

    private long CalculatePoolSize(IEnumerable<Transaction> transactions)
    {
        // Simplified size calculation for memory usage estimation
        return transactions.Sum(t => 
            200 + // Base transaction size
            (t.InputData?.Length ?? 0) + // Input data size
            (t.FromAddress?.Length ?? 0) + // Address sizes
            (t.ToAddress?.Length ?? 0) +
            (t.Signature?.Length ?? 0) // Signature size
        );
    }
}

// Supporting models and enums
public record TransactionPoolRequest
{
    public Guid? SimulationId { get; init; }
    public int MaxCount { get; init; } = 1000;
    public decimal MinimumFee { get; init; } = 0;
    public TransactionSortBy SortBy { get; init; } = TransactionSortBy.Fee;
}

public enum TransactionSortBy
{
    Fee,
    GasPrice,
    Timestamp,
    Nonce,
    Amount
}

public record TransactionPoolStats
{
    public int TotalTransactions { get; init; }
    public int PendingTransactions { get; init; }
    public int ConfirmedTransactions { get; init; }
    public int RejectedTransactions { get; init; }
    public int FailedTransactions { get; init; }
    public decimal TotalFees { get; init; }
    public decimal AverageFee { get; init; }
    public decimal HighestFee { get; init; }
    public decimal LowestFee { get; init; }
    public long TotalGas { get; init; }
    public decimal AverageGasPrice { get; init; }
    public int UniqueAccounts { get; init; }
    public long PoolSizeBytes { get; init; }
    public DateTime? OldestTransaction { get; init; }
    public DateTime? NewestTransaction { get; init; }
}