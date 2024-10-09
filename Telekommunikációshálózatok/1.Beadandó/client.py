import json
import sys

class CircuitSimulator:
    def __init__(self, filename):
        with open(filename, 'r') as f:
            data = json.load(f)
        
        self.end_points = data['end-points']
        self.switches = data['switches']
        self.links = data['links']
        self.possible_circuits = data['possible-circuits']
        self.demands = data['simulation']['demands']
        self.duration = data['simulation']['duration']

        # Élek kapacitásai
        self.link_capacities = {}
        for link in self.links:
            node1, node2 = link['points']
            self.link_capacities[(node1, node2)] = link['capacity']
            self.link_capacities[(node2, node1)] = link['capacity'] 

        self.current_allocations = [] 
        self.event_counter = 0       

    def find_circuit(self, from_node, to_node):
        for circuit in self.possible_circuits:
            if circuit[0] == from_node and circuit[-1] == to_node:
                return circuit
        return None

    def check_capacity(self, path, demand):
        for i in range(len(path) - 1):
            node1 = path[i]
            node2 = path[i + 1]
            if self.link_capacities[(node1, node2)] < demand:
                return False
        return True

    def allocate_resources(self, path, demand):
        for i in range(len(path) - 1):
            node1 = path[i]
            node2 = path[i + 1]
            self.link_capacities[(node1, node2)] -= demand
            self.link_capacities[(node2, node1)] -= demand

    def release_resources(self, path, demand):
        for i in range(len(path) - 1):
            node1 = path[i]
            node2 = path[i + 1]
            self.link_capacities[(node1, node2)] += demand
            self.link_capacities[(node2, node1)] += demand

    def run_simulation(self):
        for t in range(1, self.duration + 1):
            # Felszabadítások
            for allocation in self.current_allocations[:]:
                if allocation['end-time'] == t:
                    path = allocation['path']
                    print(f"{self.event_counter + 1}. igény felszabadítás: {allocation['from']}<->{allocation['to']} st:{t}")
                    self.release_resources(path, allocation['demand'])
                    self.current_allocations.remove(allocation)
                    self.event_counter += 1
            
            # Foglalások
            for demand in self.demands:
                if demand['start-time'] == t:
                    from_node, to_node = demand['end-points']
                    demand_amount = demand['demand']
                    path = self.find_circuit(from_node, to_node)

                    if path and self.check_capacity(path, demand_amount):
                        print(f"{self.event_counter + 1}. igény foglalás: {from_node}<->{to_node} st:{t} - sikeres")
                        self.allocate_resources(path, demand_amount)
                        self.current_allocations.append({
                            'from': from_node,
                            'to': to_node,
                            'path': path,
                            'end-time': demand['end-time'],
                            'demand': demand_amount
                        })
                    else:
                        print(f"{self.event_counter + 1}. igény foglalás: {from_node}<->{to_node} st:{t} - sikertelen")
                    self.event_counter += 1

if __name__ == '__main__':
    if len(sys.argv) != 2:
        print("Használat: python3 client.py cs1.json")
        sys.exit(1)

    filename = sys.argv[1]
    simulator = CircuitSimulator(filename)
    simulator.run_simulation()
