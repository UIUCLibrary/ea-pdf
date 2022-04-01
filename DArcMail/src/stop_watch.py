#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import time
class StopWatch ():

  ######################################################################
  def __init__ (self):
    self.timer = time.time()

  ######################################################################
  def reset_timer (self):
    self.timer = time.time()

  ######################################################################
  def elapsed_time (self):
    delta   = time.time() - self.timer
    hours   = int(delta/3600)
    minutes = delta%3600
    delta   = delta-(3600*hours)
    minutes = int(delta/60)
    seconds = int(delta%60)
    return '{0}:{1}:{2}'.format(
        '{:02d}'.format(hours),
        '{:02d}'.format(minutes),
        '{:02d}'.format(seconds))



