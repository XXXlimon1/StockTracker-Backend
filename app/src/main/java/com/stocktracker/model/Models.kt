package com.stocktracker.model

import com.google.gson.annotations.SerializedName

// Auth
data class LoginRequest(
    val email: String,
    val password: String
)

data class RegisterRequest(
    val email: String,
    val password: String
)

data class AuthResponse(
    val token: String,
    val userId: Int,
    val email: String
)

// Stock
data class Stock(
    val id: Int,
    val ticker: String,
    val fullTicker: String,
    val quantity: Int,
    val purchasePrice: Double,
    val currentPrice: Double,
    val cost: Double,
    val currentValue: Double,
    val profitLoss: Double,
    val profitLossPercent: Double,
    val purchaseDate: String
)

data class StockRequest(
    val ticker: String,       // THYAO.IS formatında
    val quantity: Int,
    val purchasePrice: Double,
    val purchaseDate: String  // "2024-01-15T00:00:00"
)

// Portfolio Summary
data class PortfolioSummary(
    val totalCost: Double,
    val totalCurrentValue: Double,
    val totalProfitLoss: Double,
    val totalProfitLossPercent: Double,
    val stockCount: Int,
    val stocks: List<Stock>
)

// Stock Price
data class StockPrice(
    val ticker: String,
    val price: Double,
    val updatedAt: String
)

data class PriceHistoryPoint(
    val price: Double,
    val recordedAt: String
)

data class PriceHistory(
    val ticker: String,
    val days: Int,
    val dataPoints: Int,
    val history: List<PriceHistoryPoint>
)

// Alert
data class Alert(
    val id: Int,
    val ticker: String,
    val alertType: String,   // "PRICE_ABOVE" veya "PRICE_BELOW"
    val targetValue: Double,
    val isActive: Boolean,
    val createdAt: String,
    val triggeredAt: String?
)

data class AlertRequest(
    val ticker: String,
    val alertType: String,
    val targetValue: Double
)

// Transaction
data class Transaction(
    val id: Int,
    val ticker: String,
    val type: String,        // "BUY" veya "SELL"
    val quantity: Int,
    val price: Double,
    val date: String
)

data class TransactionRequest(
    val ticker: String,
    val type: String,
    val quantity: Int,
    val price: Double
)
