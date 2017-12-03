#!/usr/bin/python3
#
# etherdeltaclientservice.py: EtherDelta API Client Service for python3 using websockets
# ===============================================================================
#
# This service will connect to EtherDelta's WebSocket API,
# query the market, get the order book, get recent trades,
# and then it will stay connected, listening for data and
# updating the order book and the trade history as updates come in.
#
# Author: Tom Van Braeckel <tomvanbraeckel+etherdelta@gmail.com>
# License: MIT License (MIT)
#
#
# Prerequisites
# =============
#
# This script needs the EtherDelta ABI JSON file,
# expected to be found in ../contracts/etherdelta.json
# and the generic token ABI JSON file,
# expected in ../contracts/token.json
#
#
# Install dependencies on Ubuntu 16.04 Long Term Support
# ======================================================
#
# Tested with Python 3.5.2 (default, Sep 14 2017, 22:51:06)
# Python dependencies: websocket-client, web3 (after Thu Sep 28 because we need signTransaction())
#
#
# Install Python 3 virtual environment (skip this if you have Python 3 installed system-wide)
# -------------------------------------------------------------------------------------------
# sudo apt-get install virtualenv python3-virtualenv
# virtualenv -p python3 venv
# . venv/bin/activate
#
# Install the dependencies that we need:
# --------------------------------------
# sudo apt-get install python-pip
# pip install websocket-client
#
# Install web3 from source because we need signTransaction:
# ---------------------------------------------------------
# git clone https://github.com/pipermerriam/web3.py.git
# pip install -r web3.py/requirements-dev.txt
# pip install -e web3.py
#
#
# Execution:
# ==========
# For instructions on how to configure and run the taker and maker clients,
# please read the instructions at the top of their source code.

__version__ = "4.0"

import hashlib
import websocket
import _thread
import time
import json
import random
import sys
import web3

# for debugging, enable this line and add pdb.set_trace() where you want a breakpoint:
# import pdb

from web3 import Web3, HTTPProvider
from operator import itemgetter
from collections import OrderedDict

# The functions below are used for our soliditySha256() function
from web3.utils.normalizers import abi_ens_resolver
from web3.utils.abi import map_abi_data
from eth_utils import add_0x_prefix, remove_0x_prefix
from web3.utils.encoding import hex_encode_abi_type

# EtherDelta contract address
# This rarely changes.
addressEtherDelta = '0x8d12A197cB00D4747a1fe03395095ce2A5CC6819'    # etherdelta_2's contract address
# Global API interfaces
web3 = Web3(HTTPProvider('https://mainnet.infura.io/Ky03pelFIxoZdAUsr82w'))

class EtherDeltaClientService:

    # Global lists of sells, buys and trades that are always sorted and updated whenever data comes in
    orders_sells = []
    orders_buys = []
    trades = []

    # Personal order and trade books
    my_orders_sells = []
    my_orders_buys = []
    my_trades = []

    ws = None

    def getMySellOrders(self):
        return len(self.my_orders_sells)

    def getMyBuyOrderAmount(self):
        return len(self.my_orders_buys)

    def getBestSellOrder(self):
        if len(self.orders_sells) > 0:
            return self.orders_sells[0]
        else:
            return None

    def getBestBuyOrder(self):
        if len(self.orders_buys) > 0:
            return self.orders_buys[0]
        else:
            return None

    def getEtherDeltaBalance(self, token, user):
        user = Web3.toChecksumAddress(user)
        if token == 'ETH':
            balance = self.contractEtherDelta.call().balanceOf(token="0x0000000000000000000000000000000000000000", user=user)
        else:
            token = Web3.toChecksumAddress(token)
            balance = self.contractEtherDelta.call().balanceOf(token=token, user=user)
        return web3.fromWei(balance, 'ether')

    def getBalance(self, token, user):
        user = Web3.toChecksumAddress(user)
        if token == 'ETH':
            balance = web3.eth.getBalance(user)
        else:
            balance = self.contractToken.call().balanceOf(user)
        return web3.fromWei(balance, 'ether')

    def getBlockNumber(self):
        return web3.eth.blockNumber

    # Update one side of the order book by adding all non-deleted orders for our token, then deleting the deleted orders
    def updateOneSideOfOrderBook(self, myTokenKey, token, orderbook, new_orders):
        orderbook_before = len(orderbook)
        for order in new_orders:
            # Delete deleted orders
            if order.get('deleted', None) != None:
                #print("Deleting this order from the book: " + str(order))
                orderbook = [x for x in orderbook if x['id'] != order['id']]
            elif len([x for x in orderbook if x['id'] == order['id']]) > 0:
                #orderbook = [order for x in orderbook if x['id'] == order['id']]
                neworderbook = []
                for y in orderbook:
                    if y['id'] == order['id']: neworderbook.append(order)
                orderbook = neworderbook
            elif order[myTokenKey].lower() == token.lower() and float(order['ethAvailableVolumeBase']) >= 0.001:
                orderbook.append(order)

        #print("Orderbook size changed by " + str(len(orderbook) - orderbook_before) + " orders")
        return orderbook

    def updateTradeList(self, token, tradelist, new_trades):
        tradelistsize_before = len(tradelist)
        for trade in new_trades:
            # Delete deleted trades if getting the 'deleted' property does not return the 'no such property' default value (which is 'None')
            if trade.get('deleted', None) != None:
                print("Deleting this trade from the trade list: " + str(trade))
                tradelist = [x for x in tradelist if x['txHash'].lower() != trade['txHash'].lower()]
            elif len([x for x in tradelist if x['txHash'].lower() == trade['txHash'].lower()]) > 0:
                newtradebook = []
                for y in tradelist:
                    if y['txHash'].lower() == trade['txHash'].lower(): newtradebook.append(trade)
                tradelist = newtradebook
            elif trade['tokenAddr'].lower() == token.lower():
                tradelist.append(trade)
        print("Trade list size changed by " + str(len(tradelist) - tradelistsize_before) + " trades")
        return tradelist

    def updateOrders(self, newOrders):
        self.orders_sells = self.updateOneSideOfOrderBook('tokenGive', self.token, self.orders_sells, newOrders['sells'])
        self.my_orders_sells = self.updateOneSideOfOrderBook('tokenGive', self.token, self.my_orders_sells, [x for x in newOrders['sells'] if x['user'].lower() == self.userAccount.lower()])
        self.orders_buys = self.updateOneSideOfOrderBook('tokenGet', self.token, self.orders_buys, newOrders['buys'])
        self.my_orders_buys = self.updateOneSideOfOrderBook('tokenGet', self.token, self.my_orders_buys, [x for x in newOrders['buys'] if x['user'].lower() == self.userAccount.lower()])

        self.orders_sells = sorted(self.orders_sells, key=itemgetter('ethAvailableVolume'), reverse=True)
        self.orders_sells = sorted(self.orders_sells, key=itemgetter('price'))
        self.orders_buys = sorted(self.orders_buys, key=itemgetter('ethAvailableVolume'))
        self.orders_buys = sorted(self.orders_buys, key=itemgetter('price'), reverse=True)

    def updateTrades(self, newTrades):
        self.trades = self.updateTradeList(self.token, self.trades, newTrades)
        self.my_trades = self.updateTradeList(self.token, self.my_trades, [x for x in newTrades if x['buyer'].lower() == self.userAccount.lower() or x['seller'].lower() == self.userAccount.lower()])

        self.trades = sorted(self.trades, key=itemgetter('amount'))
        self.trades = sorted(self.trades, key=itemgetter('date'), reverse=True)

    def printMyOrderBook(self):
        print()
        print('My Order book')
        print('-------------')
        self.printAnyOrderBook(self.my_orders_sells, self.my_orders_buys)

    def printOrderBook(self):
        print()
        print('Order book')
        print('----------')
        self.printAnyOrderBook(self.orders_sells, self.orders_buys)

    def printAnyOrderBook(self, sells, buys):
        ordersPerSide = 10
        topsells = reversed(sells[0:ordersPerSide])
        topbuys = buys[0:ordersPerSide]
        for sell in topsells:
            print(str(sell['price']) + " " + str(round(float(sell['ethAvailableVolume']), 3)))
        if (len(sells) > 0 and len(buys) > 0):
            spread = float(sells[0]['price']) - float(buys[0]['price'])
            print("---- Spread (" + "%.18f" % spread + ") ----")
        else:
            print('--------')
        for buy in topbuys:
            print(str(buy['price']) + " " + str(round(float(buy['ethAvailableVolume']), 3)))

    def printTrades(self):
        print()
        print('Recent trades')
        print('-------------')
        numTrades = 10
        for trade in self.trades[0:numTrades]:
            print(trade['date'] + " " + trade['side'] + " " + trade['amount'] + " @ " + trade['price'])

    def printMyTrades(self):
        print()
        print('My recent trades')
        print('----------------')
        numTrades = 10
        for trade in self.my_trades[0:numTrades]:
            print(trade['date'] + " " + trade['side'] + " " + trade['amount'] + " @ " + trade['price'])

    def printBalances(self, token, userAccount):
        print("Account balances:")
        print("=================")
        print("Wallet account balance: %.18f ETH" % self.getBalance('ETH', userAccount))
        print("Wallet token balance: %.18f tokens" % self.getBalance(token, userAccount))
        print("EtherDelta ETH balance: %.18f ETH" % self.getEtherDeltaBalance('ETH', userAccount))
        print("EtherDelta token balance: %.18f tokens" % self.getEtherDeltaBalance(token, userAccount))

    def createOrder(self, side, expires, price, amount, token, userAccount, user_wallet_private_key, randomseed = None):
        global addressEtherDelta, web3

        print("\nCreating '" + side + "' order for %.18f tokens @ %.18f ETH/token" % (amount, price))

        # Validate the input
        if len(user_wallet_private_key) != 64: raise ValueError('WARNING: user_wallet_private_key must be a hexadecimal string of 64 characters long')

        # Ensure good parameters
        token = Web3.toChecksumAddress(token)
        userAccount = Web3.toChecksumAddress(userAccount)
        user_wallet_private_key = Web3.toBytes(hexstr=user_wallet_private_key)

        # Build the order parameters
        amountBigNum = amount
        amountBaseBigNum = float(amount) * float(price)
        if randomseed != None: random.seed(randomseed)    # Seed the random number generator for unit testable results
        orderNonce = random.randint(0,10000000000)
        if side == 'sell':
            tokenGive = token
            tokenGet = '0x0000000000000000000000000000000000000000'
            amountGet = web3.toWei(amountBaseBigNum, 'ether')
            amountGive = web3.toWei(amountBigNum, 'ether')
        elif side == 'buy':
            tokenGive = '0x0000000000000000000000000000000000000000'
            tokenGet = token
            amountGet = web3.toWei(amountBigNum, 'ether')
            amountGive = web3.toWei(amountBaseBigNum, 'ether')
        else:
            print("WARNING: invalid order side, no action taken: " + str(side))

        # Serialize (according to ABI) and sha256 hash the order's parameters
        hashhex = self.soliditySha256(
            ['address', 'address', 'uint256', 'address', 'uint256', 'uint256', 'uint256'],
            [addressEtherDelta, tokenGet, amountGet, tokenGive, amountGive, expires, orderNonce]
        )
        # Sign the hash of the order's parameters with our private key (this also addes the "Ethereum Signed Message" header)
        signresult = web3.eth.account.sign(message_hexstr=hashhex, private_key=user_wallet_private_key)
        #print("Result of sign:" + str(signresult))

        orderDict = {
            'amountGet' : amountGet,
            'amountGive' : amountGive,
            'tokenGet' : tokenGet,
            'tokenGive' : tokenGive,
            'contractAddr' : addressEtherDelta,
            'expires' : expires,
            'nonce' : orderNonce,
            'user' : userAccount,
            'v' : signresult['v'],
            'r' : signresult['r'].hex(),
            's' : signresult['s'].hex(),
        }
        return orderDict

    def trade(self, order, etherAmount, user_wallet_private_key=''):
        global web3, addressEtherDelta

        # Transaction info
        maxGas = 250000
        gasPriceWei = 1000000000    # 1 Gwei
        if order['tokenGive'] == '0x0000000000000000000000000000000000000000':
            ordertype = 'buy'    # it's a buy order so we are selling tokens for ETH
            amount = etherAmount / float(order['price'])
        else:
            ordertype = 'sell'   # it's a sell order so we are buying tokens for ETH
            amount = etherAmount
        amount_in_wei = web3.toWei(amount, 'ether')

        print("\nTrading " + str(etherAmount) + " ETH of tokens (" + str(amount) + " tokens) against this " + ordertype + " order: %.10f tokens @ %.10f ETH/token" % (float(order['ethAvailableVolume']), float(order['price'])))
        print("Details about order: " + str(order))

        # trade function arguments
        kwargs = {
            'tokenGet' : Web3.toChecksumAddress(order['tokenGet']),
            'amountGet' : int(float(order['amountGet'])),
            'tokenGive' : Web3.toChecksumAddress(order['tokenGive']),
            'amountGive' : int(float(order['amountGive'])),
            'expires' : int(order['expires']),
            'nonce' : int(order['nonce']),
            'user' : Web3.toChecksumAddress(order['user']),
            'v' : order['v'],
            'r' : web3.toBytes(hexstr=order['r']),
            's' : web3.toBytes(hexstr=order['s']),
            'amount' : int(amount_in_wei),
        }

        # Bail if there's no private key
        if len(user_wallet_private_key) != 64: raise ValueError('WARNING: user_wallet_private_key must be a hexadecimal string of 64 characters long')

        # Build binary representation of the function call with arguments
        abidata = self.contractEtherDelta.encodeABI('trade', kwargs=kwargs)
        print("abidata: " + str(abidata))
        # Use the transaction count as the nonce
        nonce = web3.eth.getTransactionCount(self.userAccount)
        # Override to have same as other transaction:
        #nonce = 53
        print("nonce: " + str(nonce))
        transaction = { 'to': addressEtherDelta, 'from': self.userAccount, 'gas': maxGas, 'gasPrice': gasPriceWei, 'data': abidata, 'nonce': nonce, 'chainId': 1}
        print(transaction)
        signed = web3.eth.account.signTransaction(transaction, user_wallet_private_key)
        print("signed: " + str(signed))
        result = web3.eth.sendRawTransaction(web3.toHex(signed.rawTransaction))
        print("Transaction returned: " + str(result))
        print("\nDone! You should see the transaction show up at https://etherscan.io/tx/" + web3.toHex(result))

    # This function is very similar to Web3.soliditySha3() but there is no Web3.soliditySha256() as per November 2017
    # It serializes values according to the ABI types defined in abi_types and hashes the result with sha256.
    def soliditySha256(self, abi_types, values):
        normalized_values = map_abi_data([abi_ens_resolver(Web3)], abi_types, values)
        #print(normalized_values)
        hex_string = add_0x_prefix(''.join(
            remove_0x_prefix(hex_encode_abi_type(abi_type, value))
            for abi_type, value
            in zip(abi_types, normalized_values)
        ))
        #print(hex_string)
        hash_object = hashlib.sha256(Web3.toBytes(hexstr=hex_string))
        return hash_object.hexdigest()

    def send_getMarket(self):
        tosend = '42["getMarket",{"token":"' + self.token + '","user":"' + self.userAccount + '"}]'
        print("Sending getMarket request: " + tosend)
        self.ws.send(tosend)

    def send_message(self, argObject):
        tosend = '42["message",' + json.JSONEncoder().encode(argObject) + ']'
        print ("Sending message: " + tosend)
        self.ws.send(tosend)

    def on_cont_message(self, ws, message_string, continueflag):
        print('Received continued message from WebSocket, this is not known to happen: ' + message_string)

    def on_message(self, ws, message):
        #print('Received message from WebSocket: ' + message[0:140])
        # Only handle real data messages
        if message[:2] != "42":
            return
        # Convert message to object
        j = json.loads(message[2:])
        # Parse the message
        if 'market' in j:
            #print("Received market reply!")
            market = j[1]
            # Fill the list of trades
            if 'trades' in market:
                self.updateTrades(j[1]['trades'])
            else:
                print("WARNING: no trades found in market response from EtherDelta API, this happens from time to time but we don't really need it here so not retrying.")
            # Fill the list of orders
            if 'orders' in market:
                print("INFO: market reply contains orderbook")
                self.updateOrders(j[1]['orders'])
            else:
                print("WARNING: market response from EtherDelta API did not contain order book, this happens from time to time, retrying after a 10 second grace period...")
                time.sleep(10)
                self.send_getMarket()
        elif 'orders' in j:
            print("Got order event")
            self.updateOrders(j[1])
        elif 'trades' in j:
            print("Got trade event")
            self.updateTrades(j[1])
        elif 'funds' in j:
            print("Received funds event from EtherDelta API, no action to take.")
        elif 'messageResult' in j:
            messageResult = j[1]
            print("Received messageresult from EtherDelta API, result is: " + str(messageResult))
        else:
            print("Received an unrecognized event from the EtherDelta API, no action to take.")
            print("Message: " + str(message))

    def on_error(self, ws, error):
        print('Error:' + str(error))

    def on_ping(self, ws, ping):
        # websocket-client does not seem to call this on_ping callback when it sends a ping...
        print('Ping:' + str(ping))

    def on_pong(self, ws, pong):
        print('EtherDelta WebSocket API replied to our ping with a pong:' + str(pong))

    def on_close(self, ws):
        # The server closes the connection, regardless of our pings...
        print("WebSocket closed, reconnecting...")
        ws.close()          # Ensure it is really closed
        time.sleep(5)       # Grace period, just being polite
        self.websocket_connect()

    def on_open(self, ws):
        #print("EtherDelta WebSocket connected")
        if len(self.orders_sells) == 0:
            self.send_getMarket()

    def websocket_connect(self):
        self.ws = websocket.WebSocketApp(
            "wss://socket.etherdelta.com/socket.io/?transport=websocket",
                                  on_message = self.on_message,
                                  on_ping = self.on_ping,
                                  on_pong = self.on_pong,
                                  on_error = self.on_error,
                                  on_close = self.on_close)
        self.ws.on_open = self.on_open
        # The API seems to close the connection, even when we send a periodic ping
        self.ws.run_forever(ping_interval=10)

    def start(self, userAccount, token):
        # Most of web3's functions need checksummed addresses
        self.userAccount = Web3.toChecksumAddress(userAccount)
        self.token = Web3.toChecksumAddress(token)

        # Load the ABI of the ERC20 token
        with open('../contracts/token.json', 'r') as token_abi_definition:
            token_abi = json.load(token_abi_definition)
        self.contractToken = web3.eth.contract(address=self.token, abi=token_abi)

        # Start the main thread
        def run(*args):
            #print('Starting WebSocket version ' + websocket.__version__)
            self.websocket_connect()
            while True:
                time.sleep(10)
                print("EtherDeltaClientService thread running...")
        _thread.start_new_thread(run, ())

    def __init__(self):
        global addressEtherDelta

        addressEtherDelta = Web3.toChecksumAddress(addressEtherDelta)

        # Load the ABI of the EtherDelta contract
        with open('../contracts/etherdelta.json', 'r') as abi_definition:
            abiEtherDelta = json.load(abi_definition)
        self.contractEtherDelta = web3.eth.contract(address=addressEtherDelta, abi=abiEtherDelta)
