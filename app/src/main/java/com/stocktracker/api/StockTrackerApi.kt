package com.stocktracker.api

import com.stocktracker.model.*
import retrofit2.Response
import retrofit2.http.*

interface StockTrackerApi {

    // Auth
    @POST("api/Auth/register")
    suspend fun register(@Body request: RegisterRequest): Response<AuthResponse>

    @POST("api/Auth/login")
    suspend fun login(@Body request: LoginRequest): Response<AuthResponse>

    // Portfolio
    @GET("api/Stocks/summary")
    suspend fun getPortfolioSummary(): Response<PortfolioSummary>

    @POST("api/Stocks")
    suspend fun addStock(@Body stock: StockRequest): Response<Any>

    @DELETE("api/Stocks/{id}")
    suspend fun deleteStock(@Path("id") id: Int): Response<Any>

    // Prices
    @GET("api/StockPrice")
    suspend fun getAllPrices(): Response<List<StockPrice>>

    @GET("api/StockPrice/{ticker}/history")
    suspend fun getPriceHistory(
        @Path("ticker") ticker: String,
        @Query("days") days: Int = 7
    ): Response<PriceHistory>

    // Alerts
    @GET("api/Alerts")
    suspend fun getAlerts(): Response<List<Alert>>

    @POST("api/Alerts")
    suspend fun createAlert(@Body alert: AlertRequest): Response<Alert>

    @DELETE("api/Alerts/{id}")
    suspend fun deleteAlert(@Path("id") id: Int): Response<Any>

    // Transactions
    @GET("api/Transactions")
    suspend fun getTransactions(): Response<List<Transaction>>

    @POST("api/Transactions")
    suspend fun addTransaction(@Body transaction: TransactionRequest): Response<Any>
}
