#!/usr/bin/python3
#
# taker.py: EtherDelta taker client
# =================================
#
# This client waits until the EtherDelta order book is available,
# and then buys the best (cheapest) sell order from the book.
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
# python taker.py

import time
import sys
import configparser

from etherdeltaclientservice import EtherDeltaClientService

if __name__ == "__main__":
    print("taker.py: EtherDelta taker client")
    print("=================================")
    print("More info and details in this script's source code.")
    time.sleep(5)

    # Load config
    config = configparser.ConfigParser()
    config.read('config.ini')
    userAccount = config['DEFAULT']['user_wallet_public_key']
    user_wallet_private_key = config['DEFAULT']['user_wallet_private_key']
    token = config['DEFAULT']['token_to_trade']

    es = EtherDeltaClientService()
    es.start(userAccount, token)
    print("EtherDeltaService started")

    es.printBalances(token, userAccount)

    while es.getBestSellOrder() == None:
        print("Waiting until best sell order to buy is known...")
        time.sleep(10)

    es.printOrderBook()
    es.printTrades()

    # Buy best order
    order = es.getBestSellOrder()
    es.trade(order, 0.0001, user_wallet_private_key)

    while True:
        es.printMyOrderBook()
        time.sleep(30)

    print("taker.py exiting")
    sys.exit()
