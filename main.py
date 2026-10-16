class Point:
    def __init__(self, x, y):
        self.x = x
        self.y = y

    def __repr__(self):
        return f"({self.x}, {self.y})"

while True:
    ratioA = int(input("ratio a:"))
    ratioB = int(input("ratio b:"))

    ratio = ratioA / (ratioA + ratioB)

    a = Point(int(input("point1 a:")), int(input("point1 b:")))
    b = Point(int(input("point2 a:")), int(input("point2 b:")))

    result = Point(0, 0)

    result.x = a.x + ((b.x - a.x) * ratio)
    result.y = a.y + ((b.y - a.y) * ratio)

    print(result)