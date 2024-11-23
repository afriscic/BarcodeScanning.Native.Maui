#include <arm_neon.h>
#include <stdint.h>

extern "C" int InvertBytes(uint8_t* data, int length) {
    if (!data || length <= 0) {
        return -1;
    }

    const int vectorSize = 64;
    int i;

    try {
        for (i = 0; i <= length - vectorSize; i += vectorSize) {
            uint8x16x4_t vec = vld1q_u8_x4(data + i);
            
            vec.val[0] = vmvnq_u8(vec.val[0]);
            vec.val[1] = vmvnq_u8(vec.val[1]);
            vec.val[2] = vmvnq_u8(vec.val[2]);
            vec.val[3] = vmvnq_u8(vec.val[3]);

            vst1q_u8_x4(data + i, vec);
        }

        for (; i < length; i++) {
            data[i] = ~data[i];
        }
    }
    catch (...) {
        return -1;
    }

    return 0;
}