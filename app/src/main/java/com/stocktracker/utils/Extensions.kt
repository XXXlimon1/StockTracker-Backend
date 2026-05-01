package com.stocktracker.utils

import java.text.NumberFormat
import java.util.Locale

fun Double.toTurkishLira(): String {
    val format = NumberFormat.getNumberInstance(Locale("tr", "TR"))
    format.minimumFractionDigits = 2
    format.maximumFractionDigits = 2
    return "₺${format.format(this)}"
}

fun Double.toPercent(): String {
    val sign = if (this >= 0) "+" else ""
    return "$sign%.2f%%".format(this)
}

fun Double.isPositive() = this >= 0
