select count(1) ct, status, 'ham derlber' as name, 123312 test
from gis.cfg_wildfire_status cws
group by status 