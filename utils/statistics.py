import math


K = 40.0
initial_elo = 1500
D = 3000.0


numbers = {0: "One", 1: "Two", 2: "Three", 3: "Four"}


def probability(other, me):
    return 1.0 / (1.0 + math.pow(10, K * (other - me) / D))


def calc_elo(elo, elo_other, first_won):
    pa = probability(elo_other, elo)
    delta_ra = K * (first_won - pa)
    return delta_ra
