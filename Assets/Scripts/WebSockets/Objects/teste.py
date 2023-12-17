import matplotlib.pyplot as plt
ali = [[
  -975453.8918469656,
  4823406.562075008
],
[
  -975437.8952361388,
  4823405.896466235
],
[
  -975437.205055296,
  4823422.203893919
],
[
  -975439.7987994313,
  4823422.305182371
],
[
  -975439.5650285007,
  4823428.0207467945
],
[
  -975447.5021081942,
  4823428.353551911
],
[
  -975447.6245596341,
  4823425.5319436565
],
[
  -975453.0792146829,
  4823425.7634602
],
[
  -975453.8918469656,
  4823406.562075008
]]
# reference is at 0,0 lets shift it to the first point
for i in range(len(ali)):
    ali[i][0] = ali[i][0] + 975453.8918469656
    ali[i][1] = ali[i][1] - 4823406.562075008
  
print(ali)

x_coords = [point[0] for point in ali]
y_coords = [point[1] for point in ali]

# Plot the points
plt.plot(x_coords, y_coords, marker='o')
#add a marker or something containing the index of the array
for i in range(len(ali)):
    plt.text(x_coords[i], y_coords[i], str(i))
    
plt.title('Shifted Points')
plt.xlabel('X-axis')
plt.ylabel('Y-axis')
plt.grid(True)
plt.show()

