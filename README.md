This is a personal project that uses the 20 smooth moving average and the 50 trading strategy to trade the BTCUSDT cryptocurrency pair on the Binance exchange.

# How it works:
A Binance Websocket REST endpoint is targeted to collect data about the BTCUSDT 1m candle chart. Once the data is collected the smooth moving averages are calculated. Once the averages are calculated the 20 and the 50 average need to pass each other for a trade to open and once they pass again the trade will close. The percentage gain or loss is then added to the cash amount.

# What I learned:
-How to parse JSON
-How to calculate smooth moving averages with live data
-How to save CVS files with custom names to my desktop from google sheets
-How to use Restful endpoints
-What JSON is
-How to read other peoples code more effectively

# Disclaimer:
I am still working on this project to make a more efficient bot but the main goal is achieved, which was to make a trading bot.
