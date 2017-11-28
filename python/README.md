EtherDelta Decentralized Cryptocurrency Exchange API Client Service for python3 using WebSockets
================================================================================================

This service will connect to EtherDelta's WebSocket API,
query the market, get the order book, get recent trades,
and then it will stay connected, listening for data and
updating the order book and the trade history as updates come in.

Author: Tom Van Braeckel <tomvanbraeckel+etherdelta@gmail.com>
License: MIT License (MIT)


Prerequisites
=============

This script needs the EtherDelta ABI JSON file,
expected to be found in contracts/etherdelta.json
and the generic token ABI JSON file,
expected in contracts/token.json


Install dependencies on Ubuntu 16.04 Long Term Support
======================================================

Tested with Python 3.5.2 (default, Sep 14 2017, 22:51:06)
Python dependencies: websocket-client, web3 (after Thu Sep 28 because we need signTransaction())


Install Python 3 virtual environment (skip this if you have Python 3 installed system-wide)
-------------------------------------------------------------------------------------------
sudo apt-get install virtualenv python3-virtualenv
virtualenv -p python3 venv
. venv/bin/activate

Install the dependencies that we need:
--------------------------------------
sudo apt-get install python-pip
pip install websocket-client

Install web3 from source because we need signTransaction:
---------------------------------------------------------
git clone https://github.com/pipermerriam/web3.py.git
pip install -r web3.py/requirements-dev.txt
pip install -e web3.py


Execution:
==========
For instructions on how to configure and run the taker and maker clients,
please read the instructions at the top of their source code.
