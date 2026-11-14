import { RedisDataType as Type } from '../../../../models/redis-keys.types';
import { serializeRedisValue } from './serialize-redis-value';

describe('Redis value serialization', () => {
  it('retains collection shape and ordering for every wire type', () => {
    expect(serializeRedisValue(Type.String, '007')).toEqual({ value: '007' });
    expect(serializeRedisValue(Type.List, ['b', 'a', 'a'])).toEqual({ items: ['b', 'a', 'a'] });
    expect(serializeRedisValue(Type.Set, ['b', 'a'])).toEqual({ members: ['b', 'a'] });
    expect(
      serializeRedisValue(Type.Hash, [
        { key: 'x', value: 'a' },
        { key: 'x', value: 'b' }
      ])
    ).toEqual({ fields: { x: 'b' } });
    expect(serializeRedisValue(Type.SortedSet, [{ member: 'first', score: -1.5 }])).toEqual({
      membersWithScores: { first: -1.5 }
    });
    expect(
      serializeRedisValue(Type.Stream, [{ id: '', fields: [{ key: ' x ', value: 'v' }] }])
    ).toEqual({ entries: [{ id: '*', fields: { x: 'v' }, values: { x: 'v' } }] });
  });
});
