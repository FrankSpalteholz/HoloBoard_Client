#import <Foundation/Foundation.h>
#import <ARKit/ARKit.h>

@interface ARKitHandTrackingHelper : NSObject
+ (instancetype)sharedInstance;
- (bool)isHandTrackingAvailable;
- (void)initialize;
- (bool)isHandTracked;
- (void)getHandPosition:(float*)x y:(float*)y z:(float*)z;
@end

@implementation ARKitHandTrackingHelper {
    ARSession *_arSession;
    bool _isInitialized;
    bool _isHandTracked;
    simd_float3 _handPosition;
}

+ (instancetype)sharedInstance {
    static ARKitHandTrackingHelper *instance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        instance = [[ARKitHandTrackingHelper alloc] init];
    });
    return instance;
}

- (instancetype)init {
    self = [super init];
    if (self) {
        _isInitialized = false;
        _isHandTracked = false;
        _handPosition = simd_make_float3(0, 0, 0);
    }
    return self;
}

- (bool)isHandTrackingAvailable {
    if (@available(iOS 14.0, *)) {
        return [ARBodyTrackingConfiguration supportsHandTracking];
    }
    return false;
}

- (void)initialize {
    if (_isInitialized) return;
    
    if (@available(iOS 14.0, *)) {
        _arSession = [ARSession new];
        ARBodyTrackingConfiguration *configuration = [ARBodyTrackingConfiguration new];
        configuration.handTracking = YES;
        
        [_arSession runWithConfiguration:configuration];
        
        // Set up a delegate to receive ARFrame updates
        [self setupSessionDelegate];
        
        _isInitialized = true;
        NSLog(@"ARKit Hand Tracking initialized");
    } else {
        NSLog(@"iOS 14.0 or later is required for hand tracking");
    }
}

- (void)setupSessionDelegate {
    if (@available(iOS 14.0, *)) {
        // Set up a callback to process frames
        __weak ARKitHandTrackingHelper *weakSelf = self;
        id<ARSessionDelegate> delegate = [[NSObject alloc] init];
        
        [_arSession addObserver:self forKeyPath:@"currentFrame" options:NSKeyValueObservingOptionNew context:NULL];
    }
}

- (void)observeValueForKeyPath:(NSString *)keyPath ofObject:(id)object change:(NSDictionary<NSKeyValueChangeKey,id> *)change context:(void *)context {
    if ([keyPath isEqualToString:@"currentFrame"] && object == _arSession) {
        [self processCurrentFrame];
    }
}

- (void)processCurrentFrame {
    if (@available(iOS 14.0, *)) {
        ARFrame *currentFrame = _arSession.currentFrame;
        if (!currentFrame) return;
        
        // Reset tracking state
        _isHandTracked = false;
        
        // Process anchors to find hand tracking data
        for (ARAnchor *anchor in currentFrame.anchors) {
            if ([anchor isKindOfClass:[ARBodyAnchor class]]) {
                ARBodyAnchor *bodyAnchor = (ARBodyAnchor *)anchor;
                
                if (bodyAnchor.handTracking != nil) {
                    // Check if left or right hand is tracked
                    if (bodyAnchor.handTracking.left != nil) {
                        [self processHandData:bodyAnchor.handTracking.left];
                        _isHandTracked = true;
                    } 
                    else if (bodyAnchor.handTracking.right != nil) {
                        [self processHandData:bodyAnchor.handTracking.right];
                        _isHandTracked = true;
                    }
                }
            }
        }
    }
}

- (void)processHandData:(ARHand *)hand API_AVAILABLE(ios(14.0)) {
    if (hand == nil) return;
    
    // Get wrist position as the main hand position
    ARHandJointTransform wristTransform = [hand transformForJoint:ARHandJointWrist];
    _handPosition = wristTransform.localPosition;
}

- (bool)isHandTracked {
    return _isHandTracked;
}

- (void)getHandPosition:(float*)x y:(float*)y z:(float*)z {
    if (x != NULL) *x = _handPosition.x;
    if (y != NULL) *y = _handPosition.y;
    if (z != NULL) *z = _handPosition.z;
}

@end

// C interface for Unity to call
extern "C" {
    bool _ARKitHandTrackingIsAvailable() {
        return [[ARKitHandTrackingHelper sharedInstance] isHandTrackingAvailable];
    }
    
    void _ARKitHandTrackingInitialize() {
        [[ARKitHandTrackingHelper sharedInstance] initialize];
    }
    
    bool _ARKitHandTrackingIsHandTracked() {
        return [[ARKitHandTrackingHelper sharedInstance] isHandTracked];
    }
    
    void _ARKitHandTrackingGetHandPosition(float* x, float* y, float* z) {
        [[ARKitHandTrackingHelper sharedInstance] getHandPosition:x y:y z:z];
    }
}