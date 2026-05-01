package com.stocktracker.ui

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.navigation.fragment.NavHostFragment
import androidx.navigation.ui.setupWithNavController
import com.stocktracker.R
import com.stocktracker.api.RetrofitClient
import com.stocktracker.databinding.ActivityMainBinding
import com.stocktracker.utils.SessionManager

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        RetrofitClient.init(this)

        val navHostFragment = supportFragmentManager
            .findFragmentById(R.id.nav_host_fragment) as NavHostFragment
        val navController = navHostFragment.navController

        // Bottom navigation sadece giriş yapıldıysa göster
        binding.bottomNav.setupWithNavController(navController)

        navController.addOnDestinationChangedListener { _, destination, _ ->
            when (destination.id) {
                R.id.loginFragment, R.id.registerFragment -> {
                    binding.bottomNav.visibility = android.view.View.GONE
                }
                else -> {
                    if (SessionManager.isLoggedIn(this)) {
                        binding.bottomNav.visibility = android.view.View.VISIBLE
                    }
                }
            }
        }
    }
}
