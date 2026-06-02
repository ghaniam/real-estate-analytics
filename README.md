# real-estate-analytics

## Rationale

Paging: Through testing I noticed the API always returns a maximum of 25 objects per response regardless of the pagesize parameter. When pagesize exceeds 25, VolgendeUrl appears to skip objects since it advances to the next logical page rather than the next physical response. I'm therefore assuming pagesize=25 is the safest approach and will be using that throughout