Generated 50 level JSON files.
Rules used:
- NumQueue = number of columns/queues/lanes.
- NumPerRow = number of rows/items per queue.
- Map is column-major, bottom-to-top: index = queueIndex * NumPerRow + rowIndexFromBottom.
- -1 = empty lane cell; 0 = rainbow/white car; 1=red,2=green,3=blue,4=yellow,5=pink,6=orange,7=purple.
- Each level includes exactly one 0 rainbow cell; DummyType is the color represented by the rainbow cell.
- Empty lanes are full columns of -1.
- MaxMove has been updated from the 50 Moves values provided by the user, in level order.
