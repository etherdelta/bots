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

# These are from the buy side of the order book because they have tokenGet so they are buying token and giving ETH for it
item1 = {'expires': '104468244', 'amount': '1e+24', 'r': '0x9bd8cb2564ba8369e3ebbd5af9c3638e0cb2a929e8d5683d60baa523d3a1056e', 'updated': '2017-11-20T06:06:11.498Z', 'ethAvailableVolumeBase': '0.09999999998012342', 'v': 27, 'availableVolume': '9.9999999980123425774079e+23', 'nonce': '1950327344', 'ethAvailableVolume': '999999.9998012343', 'user': '0x1a4cfe7277bfdbd108ef42a3db9e8a1c05428c6c', 'tokenGet': '0x219218f117dc9348b358b8471c55a073e5e0da0b', 'availableVolumeBase': '99999999980123420', 'amountGet': '1e+24', 's': '0x5d287a474c8573e8d369eaf1067cfae6577ca8846044dc7ef014dc3e49479790', 'price': '0.0000001', 'amountFilled': None, 'id': '44fb9c61e6ffc5b4bd340f04f02e2bc6c3adb33514a091131fcc3a7d28c40374_buy', 'amountGive': '100000000000000000', 'tokenGive': '0x0000000000000000000000000000000000000000'}
item1_deleted = {'deleted': True, 'expires': '104468244', 'amount': '1e+24', 'r': '0x9bd8cb2564ba8369e3ebbd5af9c3638e0cb2a929e8d5683d60baa523d3a1056e', 'updated': '2017-11-20T06:06:11.498Z', 'ethAvailableVolumeBase': '0.09999999998012342', 'v': 27, 'availableVolume': '9.9999999980123425774079e+23', 'nonce': '1950327344', 'ethAvailableVolume': '999999.9998012343', 'user': '0x1a4cfe7277bfdbd108ef42a3db9e8a1c05428c6c', 'tokenGet': '0x219218f117dc9348b358b8471c55a073e5e0da0b', 'availableVolumeBase': '99999999980123420', 'amountGet': '1e+24', 's': '0x5d287a474c8573e8d369eaf1067cfae6577ca8846044dc7ef014dc3e49479790', 'price': '0.0000001', 'amountFilled': None, 'id': '44fb9c61e6ffc5b4bd340f04f02e2bc6c3adb33514a091131fcc3a7d28c40374_buy', 'amountGive': '100000000000000000', 'tokenGive': '0x0000000000000000000000000000000000000000'}
item1_same = {'expires': '104468244', 'amount': '1e+24', 'r': '0x9bd8cb2564ba8369e3ebbd5af9c3638e0cb2a929e8d5683d60baa523d3a1056e', 'updated': '2017-11-20T06:06:11.498Z', 'ethAvailableVolumeBase': '0.09999999998012342', 'v': 27, 'availableVolume': '9.9999999980123425774079e+23', 'nonce': '1950327344', 'ethAvailableVolume': '999999.9998012343', 'user': '0x1a4cfe7277bfdbd108ef42a3db9e8a1c05428c6c', 'tokenGet': '0x219218f117dc9348b358b8471c55a073e5e0da0b', 'availableVolumeBase': '99999999980123420', 'amountGet': '1e+24', 's': '0x5d287a474c8573e8d369eaf1067cfae6577ca8846044dc7ef014dc3e49479790', 'price': '0.0000001', 'amountFilled': None, 'id': '44fb9c61e6ffc5b4bd340f04f02e2bc6c3adb33514a091131fcc3a7d28c40374_buy', 'amountGive': '100000000000000000', 'tokenGive': '0x0000000000000000000000000000000000000000'}
item1_same_but_different_amountFilled = {'expires': '104468244', 'amount': '1e+24', 'r': '0x9bd8cb2564ba8369e3ebbd5af9c3638e0cb2a929e8d5683d60baa523d3a1056e', 'updated': '2017-11-20T06:06:11.498Z', 'ethAvailableVolumeBase': '0.09999999998012342', 'v': 27, 'availableVolume': '9.9999999980123425774079e+23', 'nonce': '1950327344', 'ethAvailableVolume': '999999.9998012343', 'user': '0x1a4cfe7277bfdbd108ef42a3db9e8a1c05428c6c', 'tokenGet': '0x219218f117dc9348b358b8471c55a073e5e0da0b', 'availableVolumeBase': '99999999980123420', 'amountGet': '1e+24', 's': '0x5d287a474c8573e8d369eaf1067cfae6577ca8846044dc7ef014dc3e49479790', 'price': '0.0000001', 'amountFilled': 42, 'id': '44fb9c61e6ffc5b4bd340f04f02e2bc6c3adb33514a091131fcc3a7d28c40374_buy', 'amountGive': '100000000000000000', 'tokenGive': '0x0000000000000000000000000000000000000000'}

item2 = {'expires': '104468244', 'amount': '1e+24', 'r': '0x9bd8cb2564ba8369e3ebbd5af9c3638e0cb2a929e8d5683d60baa523d3a1056e', 'updated': '2017-11-20T06:06:11.498Z', 'ethAvailableVolumeBase': '0.09999999998012342', 'v': 27, 'availableVolume': '9.9999999980123425774079e+23', 'nonce': '1950327344', 'ethAvailableVolume': '999999.9998012343', 'user': '0x1a4cfe7277bfdbd108ef42a3db9e8a1c05428c6c', 'tokenGet': '0x219218f117dc9348b358b8471c55a073e5e0da0b', 'availableVolumeBase': '99999999980123420', 'amountGet': '1e+24', 's': '0x5d287a474c8573e8d369eaf1067cfae6577ca8846044dc7ef014dc3e49479790', 'price': '0.0000001', 'amountFilled': None, 'id': '45fb9c61e6ffc5b4bd340f04f02e2bc6c3adb33514a091131fcc3a7d28c40374_buy', 'amountGive': '100000000000000000', 'tokenGive': '0x0000000000000000000000000000000000000000'}

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

