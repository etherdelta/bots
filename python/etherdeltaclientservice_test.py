import unittest

from etherdeltaclientservice import *

__version__ = "1.0"

# Fake userAccount
userAccount = '0x51df0000000000000000000000052F2e7808Ee2b'
# Fake private key
private_key = '1234567891234567891234567891234567891234567891234567891234567890'
# Real EtherDelta Contract Address
etherDeltaContractAddr = '0x8d12A197cB00D4747a1fe03395095ce2A5CC6819'
# Real GRX token
token = '0x219218f117dc9348b358b8471c55a073e5e0da0b'

# Orders
# These are from the buy side of the order book because they have tokenGet so they are buying token and giving ETH for it
item1 = {'expires': '104468244', 'amount': '1e+24', 'r': '0x9bd8cb2564ba8369e3ebbd5af9c3638e0cb2a929e8d5683d60baa523d3a1056e', 'updated': '2017-11-20T06:06:11.498Z', 'ethAvailableVolumeBase': '0.09999999998012342', 'v': 27, 'availableVolume': '9.9999999980123425774079e+23', 'nonce': '1950327344', 'ethAvailableVolume': '999999.9998012343', 'user': '0x1a4cfe7277bfdbd108ef42a3db9e8a1c05428c6c', 'tokenGet': '0x219218f117dc9348b358b8471c55a073e5e0da0b', 'availableVolumeBase': '99999999980123420', 'amountGet': '1e+24', 's': '0x5d287a474c8573e8d369eaf1067cfae6577ca8846044dc7ef014dc3e49479790', 'price': '0.0000001', 'amountFilled': None, 'id': '44fb9c61e6ffc5b4bd340f04f02e2bc6c3adb33514a091131fcc3a7d28c40374_buy', 'amountGive': '100000000000000000', 'tokenGive': '0x0000000000000000000000000000000000000000'}
item1_deleted = {'deleted': True, 'expires': '104468244', 'amount': '1e+24', 'r': '0x9bd8cb2564ba8369e3ebbd5af9c3638e0cb2a929e8d5683d60baa523d3a1056e', 'updated': '2017-11-20T06:06:11.498Z', 'ethAvailableVolumeBase': '0.09999999998012342', 'v': 27, 'availableVolume': '9.9999999980123425774079e+23', 'nonce': '1950327344', 'ethAvailableVolume': '999999.9998012343', 'user': '0x1a4cfe7277bfdbd108ef42a3db9e8a1c05428c6c', 'tokenGet': '0x219218f117dc9348b358b8471c55a073e5e0da0b', 'availableVolumeBase': '99999999980123420', 'amountGet': '1e+24', 's': '0x5d287a474c8573e8d369eaf1067cfae6577ca8846044dc7ef014dc3e49479790', 'price': '0.0000001', 'amountFilled': None, 'id': '44fb9c61e6ffc5b4bd340f04f02e2bc6c3adb33514a091131fcc3a7d28c40374_buy', 'amountGive': '100000000000000000', 'tokenGive': '0x0000000000000000000000000000000000000000'}
item1_same = {'expires': '104468244', 'amount': '1e+24', 'r': '0x9bd8cb2564ba8369e3ebbd5af9c3638e0cb2a929e8d5683d60baa523d3a1056e', 'updated': '2017-11-20T06:06:11.498Z', 'ethAvailableVolumeBase': '0.09999999998012342', 'v': 27, 'availableVolume': '9.9999999980123425774079e+23', 'nonce': '1950327344', 'ethAvailableVolume': '999999.9998012343', 'user': '0x1a4cfe7277bfdbd108ef42a3db9e8a1c05428c6c', 'tokenGet': '0x219218f117dc9348b358b8471c55a073e5e0da0b', 'availableVolumeBase': '99999999980123420', 'amountGet': '1e+24', 's': '0x5d287a474c8573e8d369eaf1067cfae6577ca8846044dc7ef014dc3e49479790', 'price': '0.0000001', 'amountFilled': None, 'id': '44fb9c61e6ffc5b4bd340f04f02e2bc6c3adb33514a091131fcc3a7d28c40374_buy', 'amountGive': '100000000000000000', 'tokenGive': '0x0000000000000000000000000000000000000000'}
item1_same_but_different_amountFilled = {'expires': '104468244', 'amount': '1e+24', 'r': '0x9bd8cb2564ba8369e3ebbd5af9c3638e0cb2a929e8d5683d60baa523d3a1056e', 'updated': '2017-11-20T06:06:11.498Z', 'ethAvailableVolumeBase': '0.09999999998012342', 'v': 27, 'availableVolume': '9.9999999980123425774079e+23', 'nonce': '1950327344', 'ethAvailableVolume': '999999.9998012343', 'user': '0x1a4cfe7277bfdbd108ef42a3db9e8a1c05428c6c', 'tokenGet': '0x219218f117dc9348b358b8471c55a073e5e0da0b', 'availableVolumeBase': '99999999980123420', 'amountGet': '1e+24', 's': '0x5d287a474c8573e8d369eaf1067cfae6577ca8846044dc7ef014dc3e49479790', 'price': '0.0000001', 'amountFilled': 42, 'id': '44fb9c61e6ffc5b4bd340f04f02e2bc6c3adb33514a091131fcc3a7d28c40374_buy', 'amountGive': '100000000000000000', 'tokenGive': '0x0000000000000000000000000000000000000000'}

item2 = {'expires': '104468244', 'amount': '1e+24', 'r': '0x9bd8cb2564ba8369e3ebbd5af9c3638e0cb2a929e8d5683d60baa523d3a1056e', 'updated': '2017-11-20T06:06:11.498Z', 'ethAvailableVolumeBase': '0.09999999998012342', 'v': 27, 'availableVolume': '9.9999999980123425774079e+23', 'nonce': '1950327344', 'ethAvailableVolume': '999999.9998012343', 'user': '0x1a4cfe7277bfdbd108ef42a3db9e8a1c05428c6c', 'tokenGet': '0x219218f117dc9348b358b8471c55a073e5e0da0b', 'availableVolumeBase': '99999999980123420', 'amountGet': '1e+24', 's': '0x5d287a474c8573e8d369eaf1067cfae6577ca8846044dc7ef014dc3e49479790', 'price': '0.0000001', 'amountFilled': None, 'id': '45fb9c61e6ffc5b4bd340f04f02e2bc6c3adb33514a091131fcc3a7d28c40374_buy', 'amountGive': '100000000000000000', 'tokenGive': '0x0000000000000000000000000000000000000000'}

# Trades
trade_sellside = {"txHash":"0xddbe429d859666217ef28a2abc74832f0bfe340f7f772058d5e885d54bff8312","date":"2017-11-15T18:32:08.000Z","price":"0.00491","side":"sell","amount":"546","amountBase":"2.68086","buyer":"0xabbb9f579d421a72301abffbfa85b7caa998f7bd","seller":"0xde57392f4ee115dfd457404215ecb590bf6c9fc2","tokenAddr":"0x219218f117dc9348b358b8471c55a073e5e0da0b"}
trade_buyside = {"txHash":"0x5a0818b4ceae815fededd82c511ccfe0f08d62d2f309cdaa7e54583ac5c4129f","date":"2017-11-15T18:32:57.000Z","price":"0.0036555","side":"buy","amount":"1200","amountBase":"4.3866","buyer":"0x3a9420663811a77617bc5809149bb2bc235facfa","seller":"0x73ced35ea2e3cab70fcb94a846ecffc008704b39","tokenAddr":"0x219218f117dc9348b358b8471c55a073e5e0da0b"}

mytrade_buyside = {"txHash":"0x5a0818b4ceae815fededd82c511ccfe0f08d62d2f309cdaa7e54583ac5c4129f","date":"2017-11-15T18:32:57.000Z","price":"0.0036555","side":"buy","amount":"1200","amountBase":"4.3866","buyer":"0x51df0000000000000000000000052F2e7808Ee2b","seller":"0x73ced35ea2e3cab70fcb94a846ecffc008704b39","tokenAddr":"0x219218f117dc9348b358b8471c55a073e5e0da0b"}
# More trades
#{"txHash":"0x5b06767bcb1ec2f87e43e2b89c0004335bea57b10d0ca298ffdfebd9a2427806","date":"2017-11-15T18:32:57.000Z","price":"0.00333","side":"buy","amount":"375","amountBase":"1.24875","buyer":"0x3a9420663811a77617bc5809149bb2bc235facfa","seller":"0xed7b254309b8e5c69409dcf40e2b119677fce144","tokenAddr":"0x56ba2ee7890461f463f7be02aac3099f6d5811a8"},{"txHash":"0x85cd0d02a436f9c186669aab46aaaf53b081867aa94239a8e300a115f63802f1","date":"2017-11-15T18:32:57.000Z","price":"0.0000427","side":"sell","amount":"20000","amountBase":"0.854","buyer":"0x8dc030c3078ab6af850d069d2a0e5ad473327202","seller":"0xca17b82dd20e95ba3c012c6a53bc5f8a706483e1","tokenAddr":"0x6aac8cb9861e42bf8259f5abdc6ae3ae89909e11"},{"txHash":"0x90d7d9b0a086843eddd12ae91ce2e5b998e2eb1850bc6b688e8e7f0e6efa5501","date":"2017-11-15T18:32:57.000Z","price":"0.002","side":"sell","amount":"597.806","amountBase":"1.195612","buyer":"0xb42c4b5eda4035d3898af3b5fe4118841b96de1c","seller":"0x23f2030563a7bbb9369eea4447eb1475f51b6bdf","tokenAddr":"0xba5f11b16b155792cf3b2e6880e8706859a8aeb6"}

# This class tests the function:

class TestTaker(unittest.TestCase):

    es = EtherDeltaClientService()

    def test_amount_orders(self):
        self.assertEqual(len(self.es.orders_sells), 0)
        self.es.orders_sells = [item1]
        self.assertEqual(len(self.es.orders_sells), 1)
        self.es.orders_sells = []
        self.assertEqual(len(self.es.orders_sells), 0)

    def test_delete_same(self):
        testlist = [item1]
        self.assertEqual(len(testlist), 1)
        testlist.remove(item1)
        self.assertEqual(len(testlist), 0)

    def test_delete_deleted(self):
        testlist = [item1]
        self.assertEqual(len(testlist), 1)
        item1_deleted.pop('deleted', None)
        testlist.remove(item1_deleted)
        self.assertEqual(len(testlist), 0)
        item1_deleted['deleted'] = True

    def test_updateOneSideOfOrderBook_delete1(self):
        orderbook = [item1]
        new_orders = [item1_deleted]
        self.assertEqual(len(orderbook), 1)
        self.assertEqual(len(new_orders), 1)
        orderbook = self.es.updateOneSideOfOrderBook('tokenGet', token, orderbook, new_orders)
        self.assertEqual(len(orderbook), 0)

    def test_updateOneSideOfOrderBook_add1(self):
        orderbook = [item1]
        new_orders = [item2]
        self.assertEqual(len(orderbook), 1)
        self.assertEqual(len(new_orders), 1)
        orderbook = self.es.updateOneSideOfOrderBook('tokenGet', token, orderbook, new_orders)
        self.assertEqual(len(orderbook), 2)
        # Test basic accessor
        self.es.orders_buys = orderbook
        amount = len(self.es.orders_sells)
        self.assertEqual(amount, 0)
        amount = len(self.es.orders_buys)
        self.assertEqual(amount, 2)

    def test_updateOneSideOfOrderBook_add_identical(self):
        orderbook = [item1]
        new_orders = [item1_same]
        self.assertEqual(len(orderbook), 1)
        self.assertEqual(len(new_orders), 1)
        orderbook = self.es.updateOneSideOfOrderBook('tokenGet', token, orderbook, new_orders)
        self.assertEqual(len(orderbook), 1)

    def test_updateOneSideOfOrderBook_add_updated(self):
        orderbook = [item1]
        new_orders = [item1_same_but_different_amountFilled]
        self.assertEqual(len(orderbook), 1)
        self.assertEqual(orderbook[0]['amountFilled'], None)
        self.assertEqual(len(new_orders), 1)
        self.assertEqual(new_orders[0]['amountFilled'], 42)
        orderbook = self.es.updateOneSideOfOrderBook('tokenGet', token, orderbook, new_orders)
        self.assertEqual(len(orderbook), 1)
        self.assertEqual(orderbook[0]['amountFilled'], 42)

    def test_updateTradeList(self):
        tradelist = [trade_sellside]
        new_trades = [trade_buyside]
        self.assertEqual(len(tradelist), 1)
        tradelist = self.es.updateTradeList(token, tradelist, new_trades)
        self.assertEqual(len(tradelist), 2)

    def test_updateTrades(self):
        tradelist = [trade_sellside]
        new_trades = [trade_buyside]
        self.assertEqual(len(tradelist), 1)
	# Setup self.es
        self.es.trades = tradelist
        self.es.my_trades = []
        self.es.token = token
        self.es.userAccount = userAccount
	# Test self.es
        self.es.updateTrades(new_trades)
        self.assertEqual(len(self.es.my_trades), 0)
        self.assertEqual(len(self.es.trades), 2)

    def test_updateTrades_my_trades(self):
        tradelist = [trade_sellside]
        new_trades = [mytrade_buyside]
	# Setup self.es
        self.es.trades = tradelist
        self.es.my_trades = []
        self.es.token = token
        self.es.userAccount = userAccount
	# Test self.es
        self.es.updateTrades(new_trades)
        self.assertEqual(len(self.es.my_trades), 1)
        self.assertEqual(len(self.es.trades), 2)	# trade should also get added to the general trade list

    def test_createSellOrder(self):
        result = self.es.createOrder('sell', 10000, 4, 2, token, userAccount, private_key, 42)

        self.assertEqual(result['nonce'], 2746317213)
        self.assertEqual(result['s'], '0x5f8037c56d94ba1065e3e63104ac12d440c6f264e8ae74fc2a376e4cae78708b')
        self.assertEqual(result['r'], '0xe93a853a22f8706233903fd1d681038b8a1a45dc548145a32370022347f89572')
        self.assertEqual(result['v'], 27)
        self.assertEqual(result['contractAddr'], etherDeltaContractAddr)

    def test_createBuyOrder(self):
        result = self.es.createOrder('buy', 10000, 4, 2, token, userAccount, private_key, 42)

        self.assertEqual(result['nonce'], 2746317213)
        self.assertEqual(result['s'], '0x12d873843641892c51e68d991c522d3bbe458f6adca0b0b62a0859807ae8b3c9')
        self.assertEqual(result['r'], '0xc287355d10c01becebaafafded85f5bc1c914e57439926bf9c2d48eca2843e15')
        self.assertEqual(result['v'], 28)
        self.assertEqual(result['contractAddr'], etherDeltaContractAddr)

if __name__ == '__main__':
    unittest.main()

