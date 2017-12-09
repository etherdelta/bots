#!/usr/bin/python3
#
# maker.py: EtherDelta maker client
# =================================
#
# This client waits until the EtherDelta order book is available,
# determines a good buy and sell price and creates new buy and
# sell orders using the EtherDelta WebSocket API.
#
# Author: Tom Van Braeckel <tomvanbraeckel+etherdelta@gmail.com>
# License: MIT License (MIT)
#
# Installation:
# =============
# This client comes with service.py that contains common EtherDelta API service facilities.
# Please see service.py for instructions on how to install it and its dependencies.
#
# Configuration:
# ==============
# Copy config.ini.example to config.ini and fill in the values.
#
# Execution:
# ==========
# . venv/bin/activate   # initialize Python 3 virtual environment, not needed if you have python 3 installed system-wide
# python maker.py

import time
import sys
import configparser

# Import our Python EtherDelta API service,
# that automatically maintains the connection,
# updates the order and trade books,
# and allows for reuse of code throughout
# different EtherDelta maker and taker clients.
from etherdeltaclientservice import EtherDeltaClientService

if __name__ == "__main__":
    print("EtherDelta API client in python using websocket")
    print("More info and details in this script's source code.")
    time.sleep(1)

    # Load config
    config = configparser.ConfigParser()
    config.read('config.ini')
    userAccount = config['DEFAULT']['user_wallet_public_key']
    user_wallet_private_key = config['DEFAULT']['user_wallet_private_key']
    token = config['DEFAULT']['token_to_trade']

    es = EtherDeltaClientService()
    es.start(userAccount, token)
    print("EtherDeltaClientService started")

    es.printBalances(token, userAccount)

    while es.getBestSellOrder() == None or es.getBestBuyOrder() == None:
        print("Waiting until best sell and buy orders are known...")
        time.sleep(4)

    best_sell = es.getBestSellOrder()
    best_buy = es.getBestBuyOrder()
    best_sell_price = float(best_sell['price'])
    best_buy_price = float(best_buy['price'])
    market_width = abs((best_buy_price - best_sell_price) / ( best_buy_price + best_sell_price ) / 2.0)
    print("Market width: %.18f ETH" % market_width)
    if market_width > 0.05:
        print("ERROR: market width is too wide, will not place orders")
        sys.exit()

    midmarket = (best_buy_price + best_sell_price) / 2.0

    es.printOrderBook()
    es.printTrades()

    ordersPerSide = 1
    sellOrdersToPlace = ordersPerSide - len(es.my_orders_sells)
    sellVolumeToPlace = 1
    buyOrdersToPlace = ordersPerSide - len(es.my_orders_buys)
    buyVolumeToPlace = 1
    expires = es.getBlockNumber() + 10

    marginfactor = 0.25

    # Create sell orders
    for sellordernr in range(1,sellOrdersToPlace+1):
        price = midmarket + sellordernr * midmarket * marginfactor
        amount = sellVolumeToPlace / sellOrdersToPlace
        order = es.createOrder('sell', expires, price, amount, token, userAccount, user_wallet_private_key)
        es.send_message(order)

    # Create buy orders
    for buyordernr in range(1,buyOrdersToPlace+1):
        price = midmarket - buyordernr * midmarket * marginfactor
        amount = float(buyVolumeToPlace) / float(price) / float(buyOrdersToPlace)
        order = es.createOrder('buy', expires, price, amount, token, userAccount, user_wallet_private_key)
        es.send_message(order)

    print("Printing the user's order book every 30 seconds...")
    while True:
        es.printMyOrderBook()
        time.sleep(30)
