import hazelcast
import time
import threading

# First
def distributed_map_demo(client):
    distributed_map = client.get_map("capitals").blocking()
    distributed_map.clear()

    for i in range(1000):
        distributed_map.put(str(i), f"Value {i}")

    print("Distributed map entries:")

    for i in range(5):
        print(f"Key {i}: {distributed_map.get(str(i))}")

# Second
def increment_without_lock(client):
    my_map = client.get_map("increment_map").blocking()
    my_map.put_if_absent("key", 0)
    
    def worker():
        for _ in range(10_000):
            current = my_map.get("key")
            new_value = current + 1
            my_map.put("key", new_value)

    threads = []

    for _ in range(3):
        t = threading.Thread(target=worker)
        threads.append(t)
        t.start()

    for t in threads:
        t.join()
    
    final_value = my_map.get("key")
    print("Final value:", final_value)

# Third
def increment_with_pessimistic_lock(client):
    my_map = client.get_map("increment_map_lock").blocking()
    my_map.put_if_absent("key", 0)
    
    def worker():
        for _ in range(10_000):
            my_map.lock("key")
            try:
                current = my_map.get("key")
                new_value = current + 1
                my_map.put("key", new_value)
            finally:
                my_map.unlock("key")
    
    threads = []
    start_time = time.time()

    for _ in range(3):
        t = threading.Thread(target=worker)
        threads.append(t)
        t.start()
    for t in threads:
        t.join()
    elapsed = time.time() - start_time
    
    final_value = my_map.get("key")
    print("Final value:", final_value)
    print("Execution time: {:.2f} seconds".format(elapsed))

# Fourth
def increment_with_optimistic_lock(client):
    my_map = client.get_map("increment_map_opt").blocking()
    my_map.put_if_absent("key", 0)
    
    def worker():
        for _ in range(10_000):
            while True:
                current = my_map.get("key")
                new_value = current + 1
                success = my_map.replace_if_same("key", current, new_value)
                if success:
                    break
    
    threads = []
    start_time = time.time()
    for _ in range(3):
        t = threading.Thread(target=worker)
        threads.append(t)
        t.start()
    for t in threads:
        t.join()
    elapsed = time.time() - start_time
    
    final_value = my_map.get("key")
    print("Final value:", final_value)
    print("Execution time: {:.2f} seconds".format(elapsed))

# Fifth
def bounded_queue_demo(client):
    queue = client.get_queue("bounded_queue").blocking()
    while not queue.is_empty():
        queue.poll()
    
    def writer():
        for i in range(1, 101):
            try:
                queue.put(i)
                print(f"Writer: added {i}")
            except Exception as e:
                print("Writer exception:", e)
            time.sleep(0.1)
    
    def reader(reader_id):
        while True:
            item = queue.poll(timeout=2)
            if item is None:
                break
            print(f"Reader {reader_id}: got {item}")
    
    writer_thread = threading.Thread(target=writer)
    reader_thread1 = threading.Thread(target=reader, args=(1,))
    reader_thread2 = threading.Thread(target=reader, args=(2,))
    
    writer_thread.start()
    reader_thread1.start()
    reader_thread2.start()
    
    writer_thread.join()
    reader_thread1.join()
    reader_thread2.join()

# Main
def main():
    client = hazelcast.HazelcastClient()
    try:
        print("=== Distributed map ===")
        distributed_map_demo(client)
        
        print("\n=== Without locks ===")
        increment_without_lock(client)
        
        print("\n=== Pessimistic lock ===")
        increment_with_pessimistic_lock(client)
        
        print("\n=== Optimistic lock ===")
        increment_with_optimistic_lock(client)
        
        print("\n=== Bounded queue ===")
        bounded_queue_demo(client)
        
    finally:
        client.shutdown()

if __name__ == "__main__":
    main()
